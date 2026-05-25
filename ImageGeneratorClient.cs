// AI Image generator. A program to generate image from AI API.
// Copyright (C) 2026  Martin Labelle
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

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

        // 🛡️ Sentinel: Hard limit on generated images (decoded bytes) to mitigate memory exhaustion / OOM
        // from oversized or malicious API responses. Input files are already capped at 20 MB in the UI layer.
        // 50 MB provides generous headroom for high-resolution outputs while preventing abuse.
        private const long MaxGeneratedImageBytes = 50 * 1024 * 1024;

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

                GeminiRequest geminiRequest = new GeminiRequest
                {
                    Contents = new[]
                    {
                        new GeminiContent
                        {
                            Parts = new[]
                            {
                                new GeminiPart { Text = prompt }
                            }
                        }
                    },
                    GenerationConfig = new GeminiGenerationConfig
                    {
                        ResponseModalities = new[] { "IMAGE" },
                        ImageConfig = new GeminiImageConfig
                        {
                            AspectRatio = aspectRatio,
                            ImageSize = resolution.ToUpperInvariant()
                        }
                    }
                };

                content = JsonContent.Create(geminiRequest, ImageGeneratorJsonContext.Default.GeminiRequest);
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

                throw new ImageGeneratorException("Une erreur de connexion réseau est survenue. Impossible de joindre l'API.", statusCode);
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
                    // ⚡ Bolt Optimization: Use JsonSerializer.DeserializeAsync instead of JsonDocument.ParseAsync.
                    // This avoids building a large DOM in memory for potentially huge payloads (like 20MB base64 images),
                    // instead streaming directly to the required string property, significantly reducing Large Object Heap allocations.
                    var result = await JsonSerializer.DeserializeAsync(responseStream, ImageGeneratorJsonContext.Default.GeminiResponse);

                    if (result?.Candidates != null && result.Candidates.Length > 0)
                    {
                        var firstCandidate = result.Candidates[0];
                        if (firstCandidate?.Content?.Parts != null && firstCandidate.Content.Parts.Length > 0)
                        {
                            var firstPart = firstCandidate.Content.Parts[0];
                            var b64Data = firstPart?.InlineData?.Data;
                            if (!string.IsNullOrEmpty(b64Data))
                            {
                                // 🛡️ Sentinel: Enforce output size limit before returning (central boundary for all callers)
                                if ((b64Data.Length * 3L / 4L) > MaxGeneratedImageBytes)
                                {
                                    throw new ImageGeneratorException("L'image générée dépasse la taille maximale autorisée.", 200);
                                }
                                return b64Data;
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
                        // 🛡️ Sentinel: Enforce output size limit before returning (central boundary for all callers)
                        if ((b64.Length * 3L / 4L) > MaxGeneratedImageBytes)
                        {
                            throw new ImageGeneratorException("L'image générée dépasse la taille maximale autorisée.", 200);
                        }
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
