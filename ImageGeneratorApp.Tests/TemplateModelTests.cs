using FluentAssertions;
using System;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class TemplateModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var model = new TemplateModel();

            // Assert
            var afterCreation = DateTime.UtcNow;

            model.Id.Should().Be(0);
            model.Key.Should().Be(string.Empty);
            model.Value.Should().Be(string.Empty);
            model.Category.Should().BeNull();
            model.Tags.Should().BeNull();
            model.UsageCount.Should().Be(0);
            model.LastUsed.Should().BeNull();

            // Check that CreatedAt and UpdatedAt are initialized within a reasonable timeframe (around when the object was created)
            model.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
            model.UpdatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        }

        [Fact]
        public void Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var model = new TemplateModel();
            var expectedDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var expectedLastUsed = new DateTime(2023, 1, 2, 12, 0, 0, DateTimeKind.Utc);

            // Act
            model.Id = 123;
            model.Key = "TestKey";
            model.Value = "TestValue";
            model.Category = "TestCategory";
            model.Tags = "tag1,tag2";
            model.UsageCount = 42;
            model.LastUsed = expectedLastUsed;
            model.CreatedAt = expectedDate;
            model.UpdatedAt = expectedDate;

            // Assert
            model.Id.Should().Be(123);
            model.Key.Should().Be("TestKey");
            model.Value.Should().Be("TestValue");
            model.Category.Should().Be("TestCategory");
            model.Tags.Should().Be("tag1,tag2");
            model.UsageCount.Should().Be(42);
            model.LastUsed.Should().Be(expectedLastUsed);
            model.CreatedAt.Should().Be(expectedDate);
            model.UpdatedAt.Should().Be(expectedDate);
        }
    }
}
