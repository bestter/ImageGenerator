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
        [InlineData("Min Status", int.MinValue)]
        [InlineData("Zero Status", 0)]
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
        [InlineData("Zero error.", 0)]
        [InlineData("Min value error.", int.MinValue)]
        [InlineData("Max value error.", int.MaxValue)]
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


        [Theory]
        [InlineData("", 200)]
        [InlineData("   ", 404)]
        [InlineData(null, 500)]
        public void Constructor_WithMessageAndStatusCodeEdgeCases_SetsPropertiesCorrectly(string? message, int expectedStatusCode)
        {
            // Act
            var exception = new ImageGeneratorException(message!, expectedStatusCode);

            // Assert
            if (message == null)
            {
                exception.Message.Should().NotBeNull(); // Exception base class provides a default message when null is passed
            }
            else
            {
                exception.Message.Should().Be(message);
            }
            exception.StatusCode.Should().Be(expectedStatusCode);
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void Constructor_ExtremelyLongMessage_SetsMessageCorrectly()
        {
            // Arrange
            string longMessage = new string('A', 100000);

            // Act
            var exception = new ImageGeneratorException(longMessage, 500);

            // Assert
            exception.Message.Should().Be(longMessage);
            exception.StatusCode.Should().Be(500);
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithNullMessageAndInnerException_HandlesGracefully()
        {
            // Arrange
            var innerException = new Exception("Inner");

            // Act
            var exception = new ImageGeneratorException(null!, innerException);

            // Assert
            exception.Message.Should().NotBeNull();
            exception.StatusCode.Should().Be(0);
            exception.InnerException.Should().Be(innerException);
        }

        [Theory]
        [InlineData("", 400)]
        [InlineData("   ", 500)]
        [InlineData(null, 200)]
        public void Constructor_WithMessageStatusCodeAndInnerExceptionEdgeCases_SetsPropertiesCorrectly(string? message, int expectedStatusCode)
        {
            // Arrange
            var innerException = new InvalidOperationException("Test inner");

            // Act
            var exception = new ImageGeneratorException(message!, expectedStatusCode, innerException);

            // Assert
            if (message == null)
            {
                exception.Message.Should().NotBeNull();
            }
            else
            {
                exception.Message.Should().Be(message);
            }
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