using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GrokImagineApp;
using Moq;
using Moq.Protected;
using Xunit;

namespace GrokImagineApp.Tests
{
    public class GrokImagineClientTests
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
            var client = new GrokImagineClient(httpClient);

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
            var client = new GrokImagineClient(httpClient);
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
            var client = new GrokImagineClient(new HttpClient());

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync(invalidKey!, "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*La clé API est requise.*");
        }

        [Fact]
        public async Task GenerateImageAsync_ApiKeyWithNewLines_ThrowsArgumentException()
        {
            // Arrange
            var client = new GrokImagineClient(new HttpClient());

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
            var client = new GrokImagineClient(new HttpClient());

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("valid_key", invalidPrompt!, "model", "1k", "16:9", "user", new List<object>());

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Un prompt est requis.*");
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsError_ParsesErrorMessageAndThrowsGrokImagineException()
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
            var client = new GrokImagineClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            var exception = await act.Should().ThrowAsync<GrokImagineException>().WithMessage("Rate limit exceeded");
            exception.Which.StatusCode.Should().Be(429);
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsErrorMessageAsObject_ParsesRawTextAndThrowsGrokImagineException()
        {
            // Arrange — message is a JSON object, not a string
            var errorJson = "{\"error\":{\"message\":{\"detail\":\"quota exceeded\",\"code\":\"rate_limit\"}}}";

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
                   Content = new StringContent(errorJson),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new GrokImagineClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert — should contain the raw JSON text of the message object, not throw InvalidOperationException
            var exception = await act.Should().ThrowAsync<GrokImagineException>();
            exception.Which.Message.Should().Contain("quota exceeded");
            exception.Which.StatusCode.Should().Be(429);
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsMalformedJson_ThrowsGrokImagineException()
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
            var client = new GrokImagineClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            await act.Should().ThrowAsync<GrokImagineException>().WithMessage("La réponse de l'API est malformée.");
        }

        [Fact]
        public async Task GenerateImageAsync_ApiReturnsValidJsonWithoutB64_ThrowsGrokImagineException()
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
            var client = new GrokImagineClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "model", "1k", "16:9", "user", new List<object>());

            // Assert
            await act.Should().ThrowAsync<GrokImagineException>().WithMessage("La réponse de l'API ne contient pas d'image valide.");
        }
    }
}
