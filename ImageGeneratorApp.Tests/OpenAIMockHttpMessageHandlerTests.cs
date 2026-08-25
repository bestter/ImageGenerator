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
using System.Net;
using System.Net.Http.Headers;
using System.Text;

using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace ImageGeneratorApp.Tests
{
    public class OpenAIMockHttpMessageHandlerTests
    {
        [Fact]
        public async Task GenerateImageAsync_EnabledMock_ReturnsLocalImageAtRequestedSize()
        {
            // Arrange
            var innerHandler = new TrackingHandler(HttpStatusCode.InternalServerError);
            var mockHandler = new OpenAIMockHttpMessageHandler(innerHandler) { Enabled = true };
            var client = new ImageGeneratorClient(new HttpClient(mockHandler));

            // Act
            string result = await client.GenerateImageAsync(
                OpenAIMockHttpMessageHandler.MockApiKey,
                "A local test image",
                "gpt-image-2",
                "2k",
                "16:9",
                "opaque_user",
                new List<ImageUrlObject>());

            // Assert
            byte[] imageBytes = Convert.FromBase64String(result);
            using ImageSharpImage image = ImageSharpImage.Load(imageBytes);
            image.Width.Should().Be(2048);
            image.Height.Should().Be(1152);
            innerHandler.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task SendAsync_EnabledMockWithInvalidPayload_ReturnsOpenAIStyleBadRequest()
        {
            // Arrange
            var innerHandler = new TrackingHandler(HttpStatusCode.InternalServerError);
            var mockHandler = new OpenAIMockHttpMessageHandler(innerHandler) { Enabled = true };
            using var client = new HttpClient(mockHandler);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/images/generations");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                OpenAIMockHttpMessageHandler.MockApiKey);
            request.Content = new StringContent(
                "{\"model\":\"gpt-image-2\",\"prompt\":\"test\",\"size\":\"1024x1024\"}",
                Encoding.UTF8,
                "application/json");

            // Act
            using HttpResponseMessage response = await client.SendAsync(
                request,
                TestContext.Current.CancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            responseBody.Should().Contain("OpenAI mock rejected the request");
            responseBody.Should().Contain("exactly model, prompt, size, and user");
            innerHandler.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task SendAsync_DisabledMock_DelegatesOpenAIRequestToInnerHandler()
        {
            // Arrange
            var innerHandler = new TrackingHandler(HttpStatusCode.Accepted);
            var mockHandler = new OpenAIMockHttpMessageHandler(innerHandler) { Enabled = false };
            using var client = new HttpClient(mockHandler);

            // Act
            using HttpResponseMessage response = await client.GetAsync(
                "https://api.openai.com/v1/images/generations",
                TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            innerHandler.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task SendAsync_EnabledMock_DelegatesNonOpenAIRequestToInnerHandler()
        {
            // Arrange
            var innerHandler = new TrackingHandler(HttpStatusCode.Accepted);
            var mockHandler = new OpenAIMockHttpMessageHandler(innerHandler) { Enabled = true };
            using var client = new HttpClient(mockHandler);

            // Act
            using HttpResponseMessage response = await client.GetAsync(
                "https://api.x.ai/v1/images/generations",
                TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            innerHandler.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task SendAsync_EnabledMockWithUnexpectedOpenAIPath_BlocksLiveRequest()
        {
            // Arrange
            var innerHandler = new TrackingHandler(HttpStatusCode.Accepted);
            var mockHandler = new OpenAIMockHttpMessageHandler(innerHandler) { Enabled = true };
            using var client = new HttpClient(mockHandler);

            // Act
            using HttpResponseMessage response = await client.GetAsync(
                "https://api.openai.com/v1/models",
                TestContext.Current.CancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            responseBody.Should().Contain("Live OpenAI requests are blocked");
            innerHandler.CallCount.Should().Be(0);
        }

        private sealed class TrackingHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;

            public TrackingHandler(HttpStatusCode statusCode)
            {
                _statusCode = statusCode;
            }

            public int CallCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _ = request;
                cancellationToken.ThrowIfCancellationRequested();
                CallCount++;
                return Task.FromResult(new HttpResponseMessage(_statusCode));
            }
        }
    }
}
#endif