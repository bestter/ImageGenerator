using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ImageGeneratorApp;
using Moq;
using Moq.Protected;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class ImageGeneratorClientTests
    {
        [Fact]
        public async Task GenerateImageAsync_ValidRequestWithoutImages_CallsGenerationsEndpointAndReturnsBase64()
        {
            // Arrange
            var expectedBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
            var responseJson = new { data = new[] { new { b64_json = expectedBase64 } } };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.Is<HttpRequestMessage>(req =>
                      req.Method == HttpMethod.Post &&
                      req.RequestUri!.ToString() == "https://api.x.ai/v1/images/generations"),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(JsonSerializer.Serialize(responseJson)),
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            var result = await client.GenerateImageAsync("dummy_key", "A cute cat", "grok-imagine-image", "1k", "16:9", "dummy_user", new List<object>());

            // Assert
            result.Should().Be(expectedBase64);
            handlerMock.Verify();
        }

        [Fact]
        public async Task GenerateImageAsync_ValidRequestWithImages_CallsEditsEndpointAndReturnsBase64()
        {
            // Arrange
            var expectedBase64 = "dummy_base_64";
            var responseJson = new { data = new[] { new { b64_json = expectedBase64 } } };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.Is<HttpRequestMessage>(req =>
                      req.Method == HttpMethod.Post &&
                      req.RequestUri!.ToString() == "https://api.x.ai/v1/images/edits"),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(JsonSerializer.Serialize(responseJson)),
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);
            var images = new List<object> { new { type = "image_url", url = "data:image/png;base64,dummy" } };

            // Act
            var result = await client.GenerateImageAsync("dummy_key", "A cute cat", "grok-imagine-image", "1k", "16:9", "dummy_user", images);

            // Assert
            result.Should().Be(expectedBase64);
            handlerMock.Verify();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GenerateImageAsync_EmptyApiKey_ThrowsArgumentException(string? invalidKey)
        {
            // Arrange
            var client = new ImageGeneratorClient(new HttpClient());

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync(invalidKey!, "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*La clé API est requise.*");
        }

        [Fact]
        public async Task GenerateImageAsync_ApiKeyWithNewLines_ThrowsArgumentException()
        {
            // Arrange
            var client = new ImageGeneratorClient(new HttpClient());

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("key\nwithnewline", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*La clé API ne doit pas contenir de retours à la ligne.*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GenerateImageAsync_EmptyPrompt_ThrowsArgumentException(string? invalidPrompt)
        {
            // Arrange
            var client = new ImageGeneratorClient(new HttpClient());

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("valid_key", invalidPrompt!, "model", "1k", "16:9", "user", new List<object>());

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Un prompt est requis.*");
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsErrorWithNonJsonContent_ThrowsGenericImageGeneratorExceptionAndDoesNotLeakHtml()
        {
            // Arrange
            var htmlErrorResponse = "<html><body><h1>502 Bad Gateway</h1></body></html>";

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.BadGateway,
                   Content = new StringContent(htmlErrorResponse),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            var exception = await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("Une erreur est survenue lors de la communication avec l'API.");
            exception.Which.StatusCode.Should().Be(502);
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsError_ParsesErrorMessageAndThrowsImageGeneratorException()
        {
            // Arrange
            var errorResponse = new { error = new { message = "Rate limit exceeded" } };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.TooManyRequests,
                   Content = new StringContent(JsonSerializer.Serialize(errorResponse)),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            var exception = await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("Rate limit exceeded");
            exception.Which.StatusCode.Should().Be(429);
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsMalformedJson_ThrowsImageGeneratorException()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("invalid json"),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("La réponse de l'API est malformée.");
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsValidJsonWithoutB64_ThrowsImageGeneratorException()
        {
            // Arrange
            var responseJson = new { data = new[] { new { other_field = "test" } } };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(JsonSerializer.Serialize(responseJson)),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("La réponse de l'API ne contient pas d'image valide.");
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsSimpleStringError_ParsesErrorMessageAndThrowsImageGeneratorException()
        {
            // Arrange
            var errorResponse = new { error = "Unauthorized access token" };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Unauthorized,
                   Content = new StringContent(JsonSerializer.Serialize(errorResponse)),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            var exception = await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("Unauthorized access token");
            exception.Which.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsNonStringMessageError_FallsBackToRawTextAndThrowsImageGeneratorException()
        {
            // Arrange
            var errorResponse = new { error = new { message = 500 } };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.InternalServerError,
                   Content = new StringContent(JsonSerializer.Serialize(errorResponse)),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            var exception = await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("500");
            exception.Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsArrayJsonError_ThrowsGenericImageGeneratorException()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.BadRequest,
                   Content = new StringContent("[1, 2, 3]"),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            var exception = await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("Une erreur est survenue lors de la communication avec l'API.");
            exception.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GenerateImageAsync_NanoBananaModel_CallsNanoBananaEndpointAndReturnsBase64()
        {
            // Arrange
            var expectedBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
            var responseJson = new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    inlineData = new
                                    {
                                        mimeType = "image/png",
                                        data = expectedBase64
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.Is<HttpRequestMessage>(req =>
                      req.Method == HttpMethod.Post &&
                      req.RequestUri!.ToString() == "https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro-image-preview:generateContent" &&
                      req.Headers.Contains("x-goog-api-key") &&
                      req.Headers.GetValues("x-goog-api-key").GetEnumerator().MoveNext() &&
                      string.Join("", req.Headers.GetValues("x-goog-api-key")) == "dummy_key"),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(JsonSerializer.Serialize(responseJson)),
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            var result = await client.GenerateImageAsync("dummy_key", "A cute cat", "nano-banana-pro", "1k", "16:9", "dummy_user", new List<object>());

            // Assert
            result.Should().Be(expectedBase64);
            handlerMock.Verify();
        }

        [Fact]
        public async Task GenerateImageAsync_NanoBananaModelWithImages_ThrowsArgumentException()
        {
            // Arrange
            var client = new ImageGeneratorClient(new HttpClient());
            var images = new List<object> { new { type = "image_url", url = "data:image/png;base64,dummy" } };

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "A cute cat", "nano-banana-pro", "1k", "16:9", "dummy_user", images);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Le modèle Nano Banana Pro ne supporte pas l'édition d'image.*");
        }

        [Fact]
        public async Task GenerateImageAsync_NanoBananaApiReturnsErrorWithMsgField_ParsesErrorMessageAndThrowsImageGeneratorException()
        {
            // Arrange
            var errorResponse = new { error = new { message = "Parameter error: invalid model specified" } };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.BadRequest,
                   Content = new StringContent(JsonSerializer.Serialize(errorResponse)),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "nano-banana-pro", "1k", "16:9", "user", new List<object>());

            // Assert
            var exception = await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("Parameter error: invalid model specified");
            exception.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GenerateImageAsync_SendAsyncThrowsHttpRequestException_ThrowsImageGeneratorException()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ThrowsAsync(new HttpRequestException("Connection refused", null, HttpStatusCode.ServiceUnavailable));

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "nano-banana-pro", "1k", "16:9", "user", new List<object>());

            // Assert
            var exception = await act.Should().ThrowAsync<ImageGeneratorException>()
                .WithMessage("Erreur de connexion réseau : Connection refused");
            exception.Which.StatusCode.Should().Be(503);
        }

        [Fact]
        public async Task GenerateImageAsync_SendAsyncThrowsIOException_ThrowsImageGeneratorExceptionWithInnerMessage()
        {
            // Arrange
            var innerException = new Exception("Connection refused by peer");
            var ioException = new System.IO.IOException("Unable to write data", innerException);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ThrowsAsync(ioException);

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "nano-banana-pro", "1k", "16:9", "user", new List<object>());

            // Assert
            var exception = await act.Should().ThrowAsync<ImageGeneratorException>()
                .WithMessage("Erreur de connexion réseau : Unable to write data (Connection refused by peer)");
            exception.Which.StatusCode.Should().Be(0);
        }
    }
}
