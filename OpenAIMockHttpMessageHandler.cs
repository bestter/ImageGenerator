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

#if DEBUG
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageGeneratorApp
{
    internal sealed class OpenAIMockHttpMessageHandler : DelegatingHandler
    {
        internal const string MockApiKey = "mock-openai-key";

        private const string OpenAIGenerationHost = "api.openai.com";
        private const string OpenAIGenerationPath = "/v1/images/generations";

        private static readonly HashSet<string> s_supportedSizes = new HashSet<string>(StringComparer.Ordinal)
        {
            "1024x1024",
            "1280x720",
            "720x1280",
            "1024x768",
            "1056x704",
            "1280x576",
            "2048x2048",
            "2048x1152",
            "1152x2048",
            "2048x1536",
            "2016x1344",
            "1920x864"
        };

        private static readonly ConcurrentDictionary<string, string> s_mockImages =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        private int _enabled;

        public OpenAIMockHttpMessageHandler()
            : this(new HttpClientHandler())
        {
        }

        internal OpenAIMockHttpMessageHandler(HttpMessageHandler innerHandler)
        {
            InnerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
        }

        internal bool Enabled
        {
            get => Volatile.Read(ref _enabled) == 1;
            set => Volatile.Write(ref _enabled, value ? 1 : 0);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!Enabled || !IsOpenAIRequest(request.RequestUri))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            if (!IsOpenAIGenerationRequest(request.RequestUri))
            {
                return CreateErrorResponse(
                    request,
                    "Live OpenAI requests are blocked while the mock is enabled.");
            }

            string? validationError = await ValidateRequestAsync(request, cancellationToken);
            if (validationError != null)
            {
                return CreateErrorResponse(request, validationError);
            }

            string requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(requestBody);
            string size = document.RootElement.GetProperty("size").GetString()!;
            string mockImageBase64 = s_mockImages.GetOrAdd(size, CreateMockImageBase64);
            string responseBody = JsonSerializer.Serialize(new
            {
                data = new[]
                {
                    new { b64_json = mockImageBase64 }
                }
            });

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                RequestMessage = request
            };
            response.Headers.Add("X-ImageGenerator-Mock", "OpenAI");
            return response;
        }

        private static bool IsOpenAIGenerationRequest(Uri? requestUri)
        {
            return requestUri != null &&
                string.Equals(requestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                IsOpenAIRequest(requestUri) &&
                string.Equals(requestUri.AbsolutePath, OpenAIGenerationPath, StringComparison.Ordinal);
        }

        private static bool IsOpenAIRequest(Uri? requestUri)
        {
            return requestUri != null &&
                string.Equals(requestUri.Host, OpenAIGenerationHost, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<string?> ValidateRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post)
            {
                return "The mock accepts only POST requests.";
            }

            if (!string.Equals(request.Headers.Authorization?.Scheme, "Bearer", StringComparison.Ordinal) ||
                !string.Equals(request.Headers.Authorization?.Parameter, MockApiKey, StringComparison.Ordinal))
            {
                return "The request does not contain the expected mock Bearer token.";
            }

            if (request.Content == null ||
                !string.Equals(request.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                return "The request content must be JSON.";
            }

            try
            {
                string requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                using JsonDocument document = JsonDocument.Parse(requestBody);
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || CountProperties(root) != 4)
                {
                    return "The OpenAI payload must contain exactly model, prompt, size, and user.";
                }

                if (!TryGetRequiredString(root, "model", out string model) || model != "gpt-image-2")
                {
                    return "The model must be gpt-image-2.";
                }

                if (!TryGetRequiredString(root, "prompt", out _))
                {
                    return "The prompt must be a non-empty string.";
                }

                if (!TryGetRequiredString(root, "size", out string size) || !s_supportedSizes.Contains(size))
                {
                    return "The size must match a supported application mapping.";
                }

                if (!TryGetRequiredString(root, "user", out _))
                {
                    return "The user identifier must be a non-empty string.";
                }
            }
            catch (JsonException)
            {
                return "The request body is not valid JSON.";
            }

            return null;
        }

        private static int CountProperties(JsonElement root)
        {
            int count = 0;
            foreach (JsonProperty property in root.EnumerateObject())
            {
                _ = property;
                count++;
            }

            return count;
        }

        private static bool TryGetRequiredString(JsonElement root, string propertyName, out string value)
        {
            value = string.Empty;
            if (!root.TryGetProperty(propertyName, out JsonElement property) ||
                property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = property.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        private static HttpResponseMessage CreateErrorResponse(HttpRequestMessage request, string message)
        {
            string responseBody = JsonSerializer.Serialize(new
            {
                error = new { message = $"OpenAI mock rejected the request: {message}" }
            });

            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                RequestMessage = request
            };
        }

        private static string CreateMockImageBase64(string size)
        {
            int separatorIndex = size.IndexOf('x');
            int width = int.Parse(size.AsSpan(0, separatorIndex));
            int height = int.Parse(size.AsSpan(separatorIndex + 1));
            int cellSize = Math.Max(1, Math.Min(width, height) / 8);
            int crossWidth = Math.Max(2, Math.Min(width, height) / 128);

            using var image = new Image<Rgba32>(width, height);
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        bool isCross = Math.Abs(x - (width / 2)) <= crossWidth ||
                            Math.Abs(y - (height / 2)) <= crossWidth;
                        bool isAccent = (((x / cellSize) + (y / cellSize)) & 1) == 0;
                        row[x] = isCross
                            ? new Rgba32(240, 240, 240)
                            : isAccent
                                ? new Rgba32(16, 163, 127)
                                : new Rgba32(24, 28, 32);
                    }
                }
            });

            using var stream = new MemoryStream();
            image.SaveAsPng(stream);
            return Convert.ToBase64String(stream.ToArray());
        }
    }
}
#endif