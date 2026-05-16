using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GrokImagineApp
{
    public class GrokImagineClient
    {
        private readonly HttpClient _httpClient;

        public GrokImagineClient(HttpClient httpClient)
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
            GrokImagineRequest requestBody = new GrokImagineRequest
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

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
            requestMessage.Content = content;

            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            using var responseStream = await response.Content.ReadAsStreamAsync();

            if (!response.IsSuccessStatusCode)
            {
                using var reader = new StreamReader(responseStream);
                var errorString = await reader.ReadToEndAsync();

                string safeErrorMessage = string.Empty;
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(errorString))
                    {
                        if (doc.RootElement.TryGetProperty("error", out JsonElement errorElement) &&
                            errorElement.TryGetProperty("message", out JsonElement messageElement))
                        {
                            safeErrorMessage = messageElement.ValueKind == JsonValueKind.String
                                ? messageElement.GetString() ?? string.Empty
                                : messageElement.GetRawText();
                        }
                    }
                }
                catch (Exception e) when (e is JsonException || e is InvalidOperationException)
                {
                    // Fallback to generic message if parsing fails
                }

                if (string.IsNullOrWhiteSpace(safeErrorMessage))
                {
                    safeErrorMessage = string.IsNullOrWhiteSpace(errorString)
                        ? "Une erreur est survenue lors de la communication avec l'API."
                        : errorString;
                }

                throw new GrokImagineException(safeErrorMessage, (int)response.StatusCode);
            }

            try
            {
                using var result = await JsonDocument.ParseAsync(responseStream);
                if (result.RootElement.TryGetProperty("data", out JsonElement dataElement) && dataElement.GetArrayLength() > 0)
                {
                    var firstItem = dataElement[0];
                    if (firstItem.TryGetProperty("b64_json", out JsonElement b64Element))
                    {
                        var b64 = b64Element.GetString();
                        if (!string.IsNullOrEmpty(b64))
                        {
                            return b64;
                        }
                    }
                }
                throw new GrokImagineException("La réponse de l'API ne contient pas d'image valide.");
            }
            catch (JsonException)
            {
                throw new GrokImagineException("La réponse de l'API est malformée.");
            }
        }
    }
}
