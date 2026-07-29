using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class GeminiModelsTests
    {
        private readonly JsonSerializerOptions _options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        [Fact]
        public void GeminiRequest_ShouldSerializeAndDeserializeCorrectly()
        {
            // Arrange
            var request = new GeminiRequest
            {
                Contents = new[]
                {
                    new GeminiContent
                    {
                        Parts = new[]
                        {
                            new GeminiPart { Text = "Test prompt" }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    ResponseModalities = new[] { "IMAGE" }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(request, _options);
            var deserialized = JsonSerializer.Deserialize<GeminiRequest>(json, _options);

            // Assert
            json.Should().Contain("\"contents\"");
            json.Should().Contain("\"generationConfig\"");
            json.Should().Contain("\"parts\"");
            json.Should().Contain("\"text\"");
            json.Should().Contain("\"Test prompt\"");
            json.Should().Contain("\"responseModalities\"");
            json.Should().Contain("\"IMAGE\"");

            deserialized.Should().NotBeNull();
            deserialized!.Contents.Should().NotBeNullOrEmpty();
            deserialized.Contents![0].Parts.Should().NotBeNullOrEmpty();
            deserialized.Contents[0].Parts![0].Text.Should().Be("Test prompt");
            deserialized.GenerationConfig.Should().NotBeNull();
            deserialized.GenerationConfig!.ResponseModalities.Should().Contain("IMAGE");
        }

        [Fact]
        public void GeminiPart_WhenTextIsNull_ShouldNotSerializeText()
        {
            // Arrange
            var part = new GeminiPart
            {
                InlineData = new GeminiInlineData { Data = "base64data" }
            };

            // Act
            var json = JsonSerializer.Serialize(part, _options);

            // Assert
            json.Should().NotContain("\"text\"");
            json.Should().Contain("\"inlineData\"");
            json.Should().Contain("\"data\"");
            json.Should().Contain("\"base64data\"");
        }

        [Fact]
        public void GeminiPart_WhenInlineDataIsNull_ShouldNotSerializeInlineData()
        {
            // Arrange
            var part = new GeminiPart
            {
                Text = "Test prompt"
            };

            // Act
            var json = JsonSerializer.Serialize(part, _options);

            // Assert
            json.Should().NotContain("\"inlineData\"");
            json.Should().Contain("\"text\"");
            json.Should().Contain("\"Test prompt\"");
        }

        [Fact]
        public void GeminiGenerationConfig_WhenImageConfigIsNull_ShouldNotSerializeImageConfig()
        {
            // Arrange
            var config = new GeminiGenerationConfig
            {
                ResponseModalities = new[] { "TEXT" }
            };

            // Act
            var json = JsonSerializer.Serialize(config, _options);

            // Assert
            json.Should().NotContain("\"imageConfig\"");
            json.Should().Contain("\"responseModalities\"");
        }

        [Fact]
        public void GeminiGenerationConfig_ShouldSerializeAndDeserializeImageConfigCorrectly()
        {
            // Arrange
            var config = new GeminiGenerationConfig
            {
                ResponseModalities = new[] { "IMAGE" },
                ImageConfig = new GeminiImageConfig
                {
                    AspectRatio = "16:9",
                    ImageSize = "LARGE"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(config, _options);
            var deserialized = JsonSerializer.Deserialize<GeminiGenerationConfig>(json, _options);

            // Assert
            json.Should().Contain("\"imageConfig\"");
            json.Should().Contain("\"aspectRatio\"");
            json.Should().Contain("\"16:9\"");
            json.Should().Contain("\"imageSize\"");
            json.Should().Contain("\"LARGE\"");

            deserialized.Should().NotBeNull();
            deserialized!.ImageConfig.Should().NotBeNull();
            deserialized.ImageConfig!.AspectRatio.Should().Be("16:9");
            deserialized.ImageConfig.ImageSize.Should().Be("LARGE");
        }

        [Fact]
        public void GeminiResponse_ShouldDeserializeCorrectly()
        {
            // Arrange
            var json = @"{
                ""candidates"": [
                    {
                        ""content"": {
                            ""parts"": [
                                {
                                    ""text"": ""Generated text""
                                },
                                {
                                    ""inlineData"": {
                                        ""data"": ""base64encodedimage""
                                    }
                                }
                            ]
                        }
                    }
                ]
            }";

            // Act
            var response = JsonSerializer.Deserialize<GeminiResponse>(json, _options);

            // Assert
            response.Should().NotBeNull();
            response!.Candidates.Should().NotBeNullOrEmpty();
            response.Candidates.Should().HaveCount(1);

            var candidate = response.Candidates![0];
            candidate.Content.Should().NotBeNull();
            candidate.Content!.Parts.Should().NotBeNullOrEmpty();
            candidate.Content.Parts.Should().HaveCount(2);

            candidate.Content.Parts![0].Text.Should().Be("Generated text");
            candidate.Content.Parts[0].InlineData.Should().BeNull();

            candidate.Content.Parts[1].Text.Should().BeNull();
            candidate.Content.Parts[1].InlineData.Should().NotBeNull();
            candidate.Content.Parts[1].InlineData!.Data.Should().Be("base64encodedimage");
        }
    }
}