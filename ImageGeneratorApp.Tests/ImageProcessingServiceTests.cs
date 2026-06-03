using System;
using System.Threading.Tasks;

namespace ImageGeneratorApp.Tests
{
    public class ImageProcessingServiceTests
    {
        [Fact]
        public async Task SaveImageAsWebpAsync_NullSourceImageBytes_ThrowsArgumentException()
        {
            // Arrange
            var service = new ImageProcessingService();
            byte[]? sourceImageBytes = null;
            string baseFileName = "test_image";

            // Act
            Func<Task> action = async () => await service.SaveImageAsWebpAsync(sourceImageBytes!, baseFileName);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Source image bytes cannot be null or empty.*")
                .WithParameterName("sourceImageBytes");
        }

        [Fact]
        public async Task SaveImageAsWebpAsync_EmptySourceImageBytes_ThrowsArgumentException()
        {
            // Arrange
            var service = new ImageProcessingService();
            byte[] sourceImageBytes = Array.Empty<byte>();
            string baseFileName = "test_image";

            // Act
            Func<Task> action = async () => await service.SaveImageAsWebpAsync(sourceImageBytes, baseFileName);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Source image bytes cannot be null or empty.*")
                .WithParameterName("sourceImageBytes");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SaveImageAsWebpAsync_InvalidBaseFileName_ThrowsArgumentException(string? invalidFileName)
        {
            // Arrange
            var service = new ImageProcessingService();
            byte[] sourceImageBytes = new byte[] { 0x00, 0x01 }; // Dummy non-empty array

            // Act
            Func<Task> action = async () => await service.SaveImageAsWebpAsync(sourceImageBytes, invalidFileName!);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Base file name cannot be null or whitespace.*")
                .WithParameterName("baseFileName");
        }
    }
}
