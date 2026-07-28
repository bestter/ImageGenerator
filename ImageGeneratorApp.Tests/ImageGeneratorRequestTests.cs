using System.Text.Json;

namespace ImageGeneratorApp.Tests
{
    public class ImageGeneratorRequestTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var request = new ImageGeneratorRequest();

            // Assert
            request.Model.Should().BeEmpty();
            request.Prompt.Should().BeEmpty();
            request.N.Should().Be(1);
            request.Resolution.Should().BeEmpty();
            request.AspectRatio.Should().BeEmpty();
            request.User.Should().BeEmpty();
            request.ResponseFormat.Should().Be("b64_json");
            request.Image.Should().BeNull();
            request.Images.Should().BeNull();
        }

        [Fact]
        public void Properties_ShouldGetAndSetCorrectly()
        {
            // Arrange
            var request = new ImageGeneratorRequest();
            var testImage = new ImageUrlObject { Url = "test_url" };
            var testImages = new[] { new ImageUrlObject { Url = "test_url1" }, new ImageUrlObject { Url = "test_url2" } };

            // Act
            request.Model = "test-model";
            request.Prompt = "test prompt";
            request.N = 5;
            request.Resolution = "1024x1024";
            request.AspectRatio = "16:9";
            request.User = "test-user";
            request.ResponseFormat = "url";
            request.Image = testImage;
            request.Images = testImages;

            // Assert
            request.Model.Should().Be("test-model");
            request.Prompt.Should().Be("test prompt");
            request.N.Should().Be(5);
            request.Resolution.Should().Be("1024x1024");
            request.AspectRatio.Should().Be("16:9");
            request.User.Should().Be("test-user");
            request.ResponseFormat.Should().Be("url");
            request.Image.Should().BeSameAs(testImage);
            request.Images.Should().BeSameAs(testImages);
        }

        [Fact]
        public void Serialize_ShouldUseCorrectJsonPropertyNames()
        {
            // Arrange
            var request = new ImageGeneratorRequest
            {
                Model = "test-model",
                Prompt = "test prompt",
                N = 2,
                Resolution = "512x512",
                AspectRatio = "1:1",
                User = "user123",
                ResponseFormat = "b64_json",
                Image = new ImageUrlObject { Url = "url1" },
                Images = new[] { new ImageUrlObject { Url = "url2" } }
            };

            // Act
            var json = JsonSerializer.Serialize(request);

            // Assert
            json.Should().Contain("\"model\":\"test-model\"");
            json.Should().Contain("\"prompt\":\"test prompt\"");
            json.Should().Contain("\"n\":2");
            json.Should().Contain("\"resolution\":\"512x512\"");
            json.Should().Contain("\"aspect_ratio\":\"1:1\"");
            json.Should().Contain("\"user\":\"user123\"");
            json.Should().Contain("\"response_format\":\"b64_json\"");
            json.Should().Contain("\"image\":");
            json.Should().Contain("\"images\":");
        }

        [Fact]
        public void Serialize_ShouldIgnoreNullImageAndImages()
        {
            // Arrange
            var request = new ImageGeneratorRequest
            {
                Image = null,
                Images = null
            };

            // Act
            var json = JsonSerializer.Serialize(request);

            // Assert
            json.Should().NotContain("\"image\":");
            json.Should().NotContain("\"images\":");
        }
    }
}