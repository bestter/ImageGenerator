using FluentAssertions;
using System;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class ImageGeneratorExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_SetsMessageAndDefaultStatusCode()
        {
            // Arrange
            string expectedMessage = "An error occurred during image generation.";

            // Act
            var exception = new ImageGeneratorException(expectedMessage);

            // Assert
            exception.Message.Should().Be(expectedMessage);
            exception.StatusCode.Should().Be(0);
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithMessageAndStatusCode_SetsMessageAndStatusCode()
        {
            // Arrange
            string expectedMessage = "API returned an error.";
            int expectedStatusCode = 400;

            // Act
            var exception = new ImageGeneratorException(expectedMessage, expectedStatusCode);

            // Assert
            exception.Message.Should().Be(expectedMessage);
            exception.StatusCode.Should().Be(expectedStatusCode);
            exception.InnerException.Should().BeNull();
        }
    }
}