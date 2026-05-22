using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageGeneratorApp
{
    public class ImageGeneratorClient
    {
        private readonly HttpClient _httpClient;

        public ImageGeneratorClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> GenerateImageAsync(
            string apiKey,
            string prompt,
            string model,
            string resolution,
            string aspectRatio,
            string opaqueUserId,
            List<object> imagesList)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("La clé API est requise.", nameof(apiKey));

            if (apiKey.Contains("\r") || apiKey.Contains("\n"))
                throw new ArgumentException("La clé API ne doit pas contenir de retours à la ligne.", nameof(apiKey));

            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Un prompt est requis.", nameof(prompt));

            string apiUrl;
            HttpContent content;
            string authHeaderName;
            string authHeaderValue;

            if (model == "nano-banana-pro")
            {
                // nano-banana-pro does not support image editing/multi-turn
                if (imagesList != null && imagesList.Count > 0)
                {
                    throw new ArgumentException("Le modèle Nano Banana Pro ne supporte pas l'édition d'image.", nameof(imagesList));
                }
                apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro-image-preview:generateContent";
                authHeaderName = "x-goog-api-key";
                authHeaderValue = apiKey;

                var geminiRequest = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseModalities = new[] { "IMAGE" },
                        imageConfig = new
                        {
                            aspectRatio = aspectRatio,
                            imageSize = resolution.ToUpperInvariant()
                        }
                    }
                };

                content = new StringContent(JsonSerializer.Serialize(geminiRequest), Encoding.UTF8, "application/json");
            }
            else
            {
                authHeaderName = "Authorization";
                authHeaderValue = $"Bearer {apiKey}";

                ImageGeneratorRequest requestBody = new ImageGeneratorRequest
                {
                    Model = model,
                    Prompt = prompt,
                    Resolution = resolution,
                    AspectRatio = aspectRatio,
                    User = opaqueUserId,
                    N = 1,
                    ResponseFormat = "b64_json"
                };

                if (imagesList != null && imagesList.Count > 0)
                {
                    apiUrl = "https://api.x.ai/v1/images/edits";
                    if (imagesList.Count == 1)
                    {
                        requestBody.Image = imagesList[0];
                    }
                    else
                    {
                        requestBody.Images = imagesList.ToArray();
                    }
                }
                else
                {
                    apiUrl = "https://api.x.ai/v1/images/generations";
                }

                // ⚡ Bolt: Using JsonContent.Create prevents large string allocations in memory by streaming the JSON
                // directly to the request stream, which is crucial since requestBody may contain large base64 strings.
                // ⚡ Bolt: Using JsonContent.Create with Source Generated Context prevents reflection overhead
                content = JsonContent.Create(requestBody, ImageGeneratorJsonContext.Default.ImageGeneratorRequest);
            }

            using var contentToDispose = content;
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            requestMessage.Headers.Add(authHeaderName, authHeaderValue);
            requestMessage.Content = contentToDispose;

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                int statusCode = 0;
                if (ex is HttpRequestException hex && hex.StatusCode.HasValue)
                {
                    statusCode = (int)hex.StatusCode.Value;
                }

                string message = ex.InnerException != null
                    ? $"{ex.Message} ({ex.InnerException.Message})"
                    : ex.Message;

                throw new ImageGeneratorException($"Erreur de connexion réseau : {message}", statusCode);
            }

            using var responseStream = await response.Content.ReadAsStreamAsync();

            if (!response.IsSuccessStatusCode)
            {
                using var reader = new StreamReader(responseStream);
                var errorString = await reader.ReadToEndAsync();

                string safeErrorMessage = string.Empty;
                if (!string.IsNullOrWhiteSpace(errorString))
                {
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(errorString))
                        {
                            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                if (doc.RootElement.TryGetProperty("msg", out JsonElement msgElement))
                                {
                                    safeErrorMessage = msgElement.ValueKind == JsonValueKind.String
                                        ? msgElement.GetString() ?? string.Empty
                                        : msgElement.GetRawText();
                                }
                                else if (doc.RootElement.TryGetProperty("error", out JsonElement errorElement))
                                {
                                    if (errorElement.ValueKind == JsonValueKind.Object &&
                                        errorElement.TryGetProperty("message", out JsonElement messageElement))
                                    {
                                        safeErrorMessage = messageElement.ValueKind == JsonValueKind.String
                                            ? messageElement.GetString() ?? string.Empty
                                            : messageElement.GetRawText();
                                    }
                                    else if (errorElement.ValueKind == JsonValueKind.String)
                                    {
                                        safeErrorMessage = errorElement.GetString() ?? string.Empty;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Fallback to generic message if parsing or property retrieval fails
                    }
                }

                if (string.IsNullOrWhiteSpace(safeErrorMessage))
                {
                    safeErrorMessage = "Une erreur est survenue lors de la communication avec l'API.";
                }

                throw new ImageGeneratorException(safeErrorMessage, (int)response.StatusCode);
            }

            try
            {
                if (model == "nano-banana-pro")
                {
                    using var doc = await JsonDocument.ParseAsync(responseStream);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("candidates", out var candidates) &&
                        candidates.ValueKind == JsonValueKind.Array &&
                        candidates.GetArrayLength() > 0)
                    {
                        var firstCandidate = candidates[0];
                        if (firstCandidate.TryGetProperty("content", out var contentElement) &&
                            contentElement.TryGetProperty("parts", out var parts) &&
                            parts.ValueKind == JsonValueKind.Array &&
                            parts.GetArrayLength() > 0)
                        {
                            var firstPart = parts[0];
                            if (firstPart.TryGetProperty("inlineData", out var inlineData) &&
                                inlineData.TryGetProperty("data", out var dataProp))
                            {
                                var base64Data = dataProp.GetString();
                                if (!string.IsNullOrEmpty(base64Data))
                                {
                                    return base64Data;
                                }
                            }
                        }
                    }
                    throw new ImageGeneratorException("La réponse de l'API ne contient pas d'image valide.");
                }
                else
                {
                    // ⚡ Bolt Optimization: Use JsonSerializer.DeserializeAsync instead of JsonDocument.ParseAsync.
                    // This avoids building a large DOM in memory for potentially huge payloads (like 20MB base64 images),
                    // instead streaming directly to the required string property, significantly reducing Large Object Heap allocations.
                    var result = await JsonSerializer.DeserializeAsync(responseStream, ImageGeneratorJsonContext.Default.ImageGeneratorResponse);

                    var b64 = result?.Data?[0]?.B64Json;
                    if (!string.IsNullOrEmpty(b64))
                    {
                        return b64;
                    }
                    throw new ImageGeneratorException("La réponse de l'API ne contient pas d'image valide.");
                }
            }
            catch (JsonException)
            {
                throw new ImageGeneratorException("La réponse de l'API est malformée.");
            }
        }
    }
}
