import re

with open("ImageGeneratorApp.Tests/ImageGeneratorClientTests.cs", "r") as f:
    content = f.read()

new_tests = """
        [Fact]
        public async Task GenerateImageAsync_NanoBananaApiReturnsMalformedJson_ThrowsImageGeneratorException()
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
                   Content = new StringContent("invalid json for nano banana"),
               });

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ImageGeneratorClient(httpClient);

            // Act
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "nano-banana-pro", "1k", "16:9", "user", new List<ImageUrlObject>());

            // Assert
            await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("La réponse de l'API est malformée.");
        }

        [Fact]
        public async Task GenerateImageAsync_NanoBananaApiReturnsValidJsonWithoutImage_ThrowsImageGeneratorException()
        {
            // Arrange
            var responseJson = new { other_field = "test" };

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
            Func<Task> act = async () => await client.GenerateImageAsync("dummy_key", "prompt", "nano-banana-pro", "1k", "16:9", "user", new List<ImageUrlObject>());

            // Assert
            await act.Should().ThrowAsync<ImageGeneratorException>().WithMessage("La réponse de l'API ne contient pas d'image valide.");
        }
"""

# Insert before GenerateImageAsync_SendAsyncThrowsHttpRequestException_ThrowsImageGeneratorException
search = "        [Fact]\n        public async Task GenerateImageAsync_SendAsyncThrowsHttpRequestException_ThrowsImageGeneratorException()"
content = content.replace(search, new_tests + "\n" + search)

with open("ImageGeneratorApp.Tests/ImageGeneratorClientTests.cs", "w") as f:
    f.write(content)
