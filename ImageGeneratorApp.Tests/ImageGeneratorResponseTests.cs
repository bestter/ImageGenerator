using System.Text.Json;

namespace ImageGeneratorApp.Tests
{
    public class ImageGeneratorResponseTests
    {
        [Fact]
        public void Deserialize_ValidJson_PopulatesPropertiesCorrectly()
        {
            // Arrange
            string json = @"{
                ""data"": [
                    {
                        ""b64_json"": ""dummy_base64_string_1""
                    },
                    {
                        ""b64_json"": ""dummy_base64_string_2""
                    }
                ]
            }";

            // Act
            var response = JsonSerializer.Deserialize<ImageGeneratorResponse>(json);

            // Assert
            response.Should().NotBeNull();
            response!.Data.Should().NotBeNull();
            response.Data.Should().HaveCount(2);
            response.Data[0].B64Json.Should().Be("dummy_base64_string_1");
            response.Data[1].B64Json.Should().Be("dummy_base64_string_2");
        }

        [Fact]
        public void Deserialize_EmptyData_PopulatesEmptyArray()
        {
            // Arrange
            string json = @"{
                ""data"": []
            }";

            // Act
            var response = JsonSerializer.Deserialize<ImageGeneratorResponse>(json);

            // Assert
            response.Should().NotBeNull();
            response!.Data.Should().NotBeNull();
            response.Data.Should().BeEmpty();
        }

        [Fact]
        public void Deserialize_MissingData_PopulatesNull()
        {
            // Arrange
            string json = @"{
                ""other_property"": ""value""
            }";

            // Act
            var response = JsonSerializer.Deserialize<ImageGeneratorResponse>(json);

            // Assert
            response.Should().NotBeNull();
            response!.Data.Should().BeNull();
        }

        [Fact]
        public void Serialize_PopulatedObject_GeneratesCorrectJson()
        {
            // Arrange
            var response = new ImageGeneratorResponse
            {
                Data = new[]
                {
                    new ImageGeneratorResponseData { B64Json = "test_b64" }
                }
            };

            // Act
            string json = JsonSerializer.Serialize(response);

            // Assert
            json.Should().Contain(@"""data"":");
            json.Should().Contain(@"""b64_json"":""test_b64""");
        }

        [Fact]
        public void Serialize_NullData_GeneratesJsonWithNullData()
        {
            // Arrange
            var response = new ImageGeneratorResponse
            {
                Data = null
            };

            // Act
            string json = JsonSerializer.Serialize(response);

            // Assert
            json.Should().Contain(@"""data"":null");
        }
    }
}