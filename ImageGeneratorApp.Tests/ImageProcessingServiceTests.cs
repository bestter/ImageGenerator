using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class ImageProcessingServiceTests : IDisposable
    {
        private readonly ImageProcessingService _imageProcessingService;
        private string? _createdWebpPath;

        // A valid 1x1 black pixel PNG byte array to simulate an API response
        private static readonly byte[] ValidPngBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
            0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D, 0xB0, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
            0x44, 0xAE, 0x42, 0x60, 0x82
        };

        public ImageProcessingServiceTests()
        {
            _imageProcessingService = new ImageProcessingService();
        }

        public void Dispose()
        {
            // Clean up any generated test WEBP image files
            if (!string.IsNullOrEmpty(_createdWebpPath) && File.Exists(_createdWebpPath))
            {
                try { File.Delete(_createdWebpPath); } catch { }
            }
        }

[Fact]
        public async Task LoadWebpForWinFormsAsync_ShouldLoadWebpImageAsGdiPlusImage()
        {
            // Arrange
            var baseFileName = $"test_load_{Guid.NewGuid():N}";

            // Save a test WebP image first
            var savedPath = await _imageProcessingService.SaveImageAsWebpAsync(ValidPngBytes, baseFileName);
            _createdWebpPath = savedPath; // Register for automatic cleanup in Dispose()

            // Act
            using var gdiImage = await _imageProcessingService.LoadWebpForWinFormsAsync(savedPath);

            // Assert
            gdiImage.Should().NotBeNull();
            gdiImage.Width.Should().Be(1);
            gdiImage.Height.Should().Be(1);
            gdiImage.Should().BeOfType<System.Drawing.Bitmap>();
        }

[Fact]
        public async Task LoadWebpForWinFormsAsync_InvalidPaths_ThrowExceptions()
        {
            // Assert null/empty path throws ArgumentException
            Func<Task> act1 = async () => await _imageProcessingService.LoadWebpForWinFormsAsync(null!);
            await act1.Should().ThrowAsync<ArgumentException>().WithMessage("*path*");

            Func<Task> act2 = async () => await _imageProcessingService.LoadWebpForWinFormsAsync("   ");
            await act2.Should().ThrowAsync<ArgumentException>().WithMessage("*path*");

            // Assert non-existent path throws FileNotFoundException
            var nonExistentPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.webp");
            Func<Task> act3 = async () => await _imageProcessingService.LoadWebpForWinFormsAsync(nonExistentPath);
            await act3.Should().ThrowAsync<FileNotFoundException>();
        }

[Fact]
        public async Task LoadWebpForWinFormsAsync_EmptyFile_ThrowsArgumentException()
        {
            // Arrange
            var emptyFilePath = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}.webp");
            await File.WriteAllBytesAsync(emptyFilePath, Array.Empty<byte>(), TestContext.Current.CancellationToken);
            _createdWebpPath = emptyFilePath; // Register for cleanup

            try
            {
                // Act
                Func<Task> act = async () => await _imageProcessingService.LoadWebpForWinFormsAsync(emptyFilePath);

                // Assert
                await act.Should().ThrowAsync<ArgumentException>()
                    .WithMessage("File is empty.*");
            }
            finally
            {
                if (File.Exists(emptyFilePath))
                {
                    File.Delete(emptyFilePath);
                }
            }
        }

        [Fact]
        public async Task LoadWebpForWinFormsAsync_TooLargeFile_ThrowsArgumentException()
        {
            var largeFilePath = Path.Combine(Path.GetTempPath(), $"large_{Guid.NewGuid().ToString("N")}.webp");

            using (var fs = new FileStream(largeFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.SetLength(20 * 1024 * 1024 + 1);
            }
            _createdWebpPath = largeFilePath;

            try
            {
                Func<Task> act = async () => await _imageProcessingService.LoadWebpForWinFormsAsync(largeFilePath);
                await act.Should().ThrowAsync<ArgumentException>()
                    .WithMessage("File exceeds the maximum allowed size of 20 MB.*");
            }
            finally
            {
                if (File.Exists(largeFilePath))
                {
                    File.Delete(largeFilePath);
                }
            }
        }

        [Fact]
        public async Task SaveImageAsWebpAsync_NullOrEmptySourceBytes_ThrowsArgumentException()
        {
            // Arrange
            var baseFileName = "test_image";

            // Act & Assert Null Image Bytes
            Func<Task> act1 = async () => await _imageProcessingService.SaveImageAsWebpAsync(null!, baseFileName);
            await act1.Should().ThrowAsync<ArgumentException>().WithMessage("*Source image bytes cannot be null or empty.*");

            // Act & Assert Empty Image Bytes
            Func<Task> act2 = async () => await _imageProcessingService.SaveImageAsWebpAsync(Array.Empty<byte>(), baseFileName);
            await act2.Should().ThrowAsync<ArgumentException>().WithMessage("*Source image bytes cannot be null or empty.*");
        }

[Fact]
        public async Task SaveImageAsWebpAsync_NullOrWhitespaceBaseFileName_ThrowsArgumentException()
        {
            // Act & Assert Null
            Func<Task> act1 = async () => await _imageProcessingService.SaveImageAsWebpAsync(ValidPngBytes, null!);
            await act1.Should().ThrowAsync<ArgumentException>().WithMessage("*Base file name cannot be null or whitespace.*");

            // Act & Assert Whitespace
            Func<Task> act2 = async () => await _imageProcessingService.SaveImageAsWebpAsync(ValidPngBytes, "   ");
            await act2.Should().ThrowAsync<ArgumentException>().WithMessage("*Base file name cannot be null or whitespace.*");
        }

        [Fact]
        public async Task SaveImageAsWebpAsync_WithMetadata_EmbedsMetadataInWebpImage()
        {
            // Arrange
            var baseFileName = $"test_meta_{Guid.NewGuid():N}";
            var metadata = new ImageGenerationMetadata(
                Generator: "Grok Imagine",
                Prompt: "A futuristic skyline",
                ModelId: "grok-imagine-image",
                GeneratedAtUtc: DateTime.UtcNow,
                Resolution: "1024x1024",
                AspectRatio: "1:1",
                AppCreator: "GrokImagineApp"
            );

            // Act
            var savedPath = await _imageProcessingService.SaveImageAsWebpAsync(ValidPngBytes, baseFileName, metadata);
            _createdWebpPath = savedPath;

            // Assert
            File.Exists(savedPath).Should().BeTrue();
            using var loadedImage = SixLabors.ImageSharp.Image.Load(savedPath);
            loadedImage.Metadata.ExifProfile.Should().NotBeNull();
            loadedImage.Metadata.XmpProfile.Should().NotBeNull();
        }
    }
}

