using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class OpenAIImageRequestTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var request = new OpenAIImageRequest();

            // Assert
            request.Model.Should().BeEmpty();
            request.Prompt.Should().BeEmpty();
            request.Size.Should().BeEmpty();
            request.User.Should().BeEmpty();
        }

        [Fact]
        public void Properties_ShouldGetAndSetCorrectly()
        {
            // Arrange
            var request = new OpenAIImageRequest();

            // Act
            request.Model = "dall-e-3";
            request.Prompt = "A cute baby sea otter";
            request.Size = "1024x1024";
            request.User = "user-123";

            // Assert
            request.Model.Should().Be("dall-e-3");
            request.Prompt.Should().Be("A cute baby sea otter");
            request.Size.Should().Be("1024x1024");
            request.User.Should().Be("user-123");
        }

        [Fact]
        public void Serialize_ShouldUseCorrectJsonPropertyNames()
        {
            // Arrange
            var request = new OpenAIImageRequest
            {
                Model = "dall-e-3",
                Prompt = "A cute baby sea otter",
                Size = "1024x1024",
                User = "user-123"
            };

            // Act
            var json = JsonSerializer.Serialize(request);

            // Assert
            json.Should().Contain("\"model\":\"dall-e-3\"");
            json.Should().Contain("\"prompt\":\"A cute baby sea otter\"");
            json.Should().Contain("\"size\":\"1024x1024\"");
            json.Should().Contain("\"user\":\"user-123\"");
        }

        [Fact]
        public void Deserialize_ShouldBindCorrectly()
        {
            // Arrange
            var json = @"{
                ""model"": ""dall-e-3"",
                ""prompt"": ""A cute baby sea otter"",
                ""size"": ""1024x1024"",
                ""user"": ""user-123""
            }";

            // Act
            var request = JsonSerializer.Deserialize<OpenAIImageRequest>(json);

            // Assert
            request.Should().NotBeNull();
            request!.Model.Should().Be("dall-e-3");
            request.Prompt.Should().Be("A cute baby sea otter");
            request.Size.Should().Be("1024x1024");
            request.User.Should().Be("user-123");
        }

        [Fact]
        public void Deserialize_EmptyJson_ShouldUseDefaultValues()
        {
            // Arrange
            var json = "{}";

            // Act
            var request = JsonSerializer.Deserialize<OpenAIImageRequest>(json);

            // Assert
            request.Should().NotBeNull();
            request!.Model.Should().BeEmpty();
            request.Prompt.Should().BeEmpty();
            request.Size.Should().BeEmpty();
            request.User.Should().BeEmpty();
        }
    }
}
