using System;

namespace ImageGeneratorApp.Tests
{
    public class ImageGeneratorExceptionTests
    {
        [Fact]
        public void Constructor_Default_SetsDefaultValues()
        {
            // Act
            var exception = new ImageGeneratorException();

            // Assert
            exception.Message.Should().NotBeNullOrEmpty();
            exception.StatusCode.Should().Be(0);
            exception.InnerException.Should().BeNull();
        }

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
        public void Constructor_WithMessageAndInnerException_SetsMessageAndInnerException()
        {
            // Arrange
            string expectedMessage = "An error occurred.";
            var innerException = new InvalidOperationException("Inner error.");

            // Act
            var exception = new ImageGeneratorException(expectedMessage, innerException);

            // Assert
            exception.Message.Should().Be(expectedMessage);
            exception.StatusCode.Should().Be(0);
            exception.InnerException.Should().Be(innerException);
        }

        [Theory]
        [InlineData("API returned an error.", 400)]
        [InlineData("Not Found", 404)]
        [InlineData("Server Error", 500)]
        [InlineData("Negative Status", -1)]
        [InlineData("Max Status", int.MaxValue)]
        public void Constructor_WithMessageAndStatusCode_SetsMessageAndStatusCode(string expectedMessage, int expectedStatusCode)
        {
            // Act
            var exception = new ImageGeneratorException(expectedMessage, expectedStatusCode);

            // Assert
            exception.Message.Should().Be(expectedMessage);
            exception.StatusCode.Should().Be(expectedStatusCode);
            exception.InnerException.Should().BeNull();
        }

        [Theory]
        [InlineData("API returned an error with inner exception.", 500)]
        [InlineData("Another error.", 403)]
        [InlineData("Negative error.", -1)]
        public void Constructor_WithMessageStatusCodeAndInnerException_SetsAllProperties(string expectedMessage, int expectedStatusCode)
        {
            // Arrange
            var innerException = new Exception("Root cause.");

            // Act
            var exception = new ImageGeneratorException(expectedMessage, expectedStatusCode, innerException);

            // Assert
            exception.Message.Should().Be(expectedMessage);
            exception.StatusCode.Should().Be(expectedStatusCode);
            exception.InnerException.Should().Be(innerException);
        }

        [Fact]
        public void Constructor_NullMessage_HandlesGracefully()
        {
            // Act
            var exception = new ImageGeneratorException(null!);

            // Assert
            exception.Message.Should().NotBeNull();
            exception.StatusCode.Should().Be(0);
        }

        [Fact]
        public void ThrowAndCatch_PreservesExceptionProperties()
        {
            // Arrange
            string expectedMessage = "Test throw message";
            int expectedStatusCode = 401;

            // Act
            Exception? caughtException = null;
            try
            {
                throw new ImageGeneratorException(expectedMessage, expectedStatusCode);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Assert
            caughtException.Should().NotBeNull();
            caughtException.Should().BeOfType<ImageGeneratorException>();

            var typedException = (ImageGeneratorException)caughtException!;
            typedException.Message.Should().Be(expectedMessage);
            typedException.StatusCode.Should().Be(expectedStatusCode);
        }
    }
}
