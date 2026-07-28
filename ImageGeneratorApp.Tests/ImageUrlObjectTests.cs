using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace ImageGeneratorApp.Tests;

public class ImageUrlObjectTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var obj = new ImageUrlObject();

        // Assert
        obj.Type.Should().Be("image_url");
        obj.Url.Should().Be(string.Empty);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var obj = new ImageUrlObject();
        var newType = "custom_type";
        var newUrl = "https://example.com/image.png";

        // Act
        obj.Type = newType;
        obj.Url = newUrl;

        // Assert
        obj.Type.Should().Be(newType);
        obj.Url.Should().Be(newUrl);
    }

    [Fact]
    public void JsonSerializer_ShouldSerializeCorrectly()
    {
        // Arrange
        var obj = new ImageUrlObject { Url = "https://example.com/test.png" };

        // Act
        var json = JsonSerializer.Serialize(obj, ImageGeneratorJsonContext.Default.ImageUrlObject);

        // Assert
        json.Should().Contain("\"type\":\"image_url\"");
        json.Should().Contain("\"url\":\"https://example.com/test.png\"");
    }

    [Fact]
    public void JsonSerializer_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = "{\"type\":\"custom_type\",\"url\":\"https://example.com/test.png\"}";

        // Act
        var obj = JsonSerializer.Deserialize(json, ImageGeneratorJsonContext.Default.ImageUrlObject);

        // Assert
        obj.Should().NotBeNull();
        obj!.Type.Should().Be("custom_type");
        obj.Url.Should().Be("https://example.com/test.png");
    }
}
