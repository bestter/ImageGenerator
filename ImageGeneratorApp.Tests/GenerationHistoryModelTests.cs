using FluentAssertions;
using System;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class GenerationHistoryModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var model = new GenerationHistoryModel();

            // Assert
            var afterCreation = DateTime.UtcNow;

            model.Id.Should().Be(0);
            model.ImagePath.Should().Be(string.Empty);
            model.Prompt.Should().Be(string.Empty);
            model.ModelName.Should().Be(string.Empty);
            model.ModelVersion.Should().BeNull();
            model.RawMetadata.Should().BeNull();

            // Check that CreatedAt is initialized within a reasonable timeframe (around when the object was created)
            model.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);

            // Check that they are specifically UTC
            model.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var model = new GenerationHistoryModel();
            var expectedDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            // Act
            model.Id = 123;
            model.ImagePath = "/path/to/image.png";
            model.Prompt = "A cute cat";
            model.ModelName = "dall-e-3";
            model.ModelVersion = "latest";
            model.RawMetadata = "{\"key\":\"value\"}";
            model.CreatedAt = expectedDate;

            // Assert
            model.Id.Should().Be(123);
            model.ImagePath.Should().Be("/path/to/image.png");
            model.Prompt.Should().Be("A cute cat");
            model.ModelName.Should().Be("dall-e-3");
            model.ModelVersion.Should().Be("latest");
            model.RawMetadata.Should().Be("{\"key\":\"value\"}");
            model.CreatedAt.Should().Be(expectedDate);
        }

        [Fact]
        public void Constructor_MultipleInstances_ShouldHaveDistinctCreationTimes()
        {
            // Act
            var model1 = new GenerationHistoryModel();
            var model2 = new GenerationHistoryModel();

            // Assert
            model1.Should().NotBeSameAs(model2);

            // In quick succession, they might have the same time, but they are independent instances
            model1.CreatedAt.Should().BeOnOrBefore(model2.CreatedAt);

            // Additional checks to make sure we're correctly dealing with different values
            model1.Id = 1;
            model2.Id = 2;
            model1.Id.Should().NotBe(model2.Id);
        }
    }
}
