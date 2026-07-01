using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    /// <summary>
    /// Integration tests for <see cref="HistoryOrchestrator"/>, <see cref="ImageProcessingService"/>, and <see cref="GenerationHistoryRepository"/>.
    /// Uses a temporary on-disk SQLite database and cleans up saved test WEBP files.
    /// </summary>
    public class HistoryOrchestratorTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly DatabaseHelper _databaseHelper;
        private readonly GenerationHistoryRepository _repository;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly HistoryOrchestrator _orchestrator;
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

        public HistoryOrchestratorTests()
        {
            // Unique temporary database file path
            _dbPath = Path.Combine(Path.GetTempPath(), $"ImageGenerator_OrchestratorTest_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={_dbPath}";

            _databaseHelper = new DatabaseHelper(connectionString);
            _repository = new GenerationHistoryRepository(_databaseHelper);
            _imageProcessingService = new ImageProcessingService();
            _orchestrator = new HistoryOrchestrator(_imageProcessingService, _repository);
        }

        public void Dispose()
        {
            // Clean up database connection
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (File.Exists(_dbPath))
            {
                try { File.Delete(_dbPath); } catch { }
            }

            // Clean up any generated test WEBP image files
            if (!string.IsNullOrEmpty(_createdWebpPath) && File.Exists(_createdWebpPath))
            {
                try { File.Delete(_createdWebpPath); } catch { }
            }
        }

        [Fact]
        public async Task LogGenerationAsync_ShouldSaveWebpImageAndRecordMetadataInDatabase()
        {
            // Arrange
            var prompt = "A futuristic cyberpunk city with neon lights";
            var modelName = "grok-imagine-image-quality";
            var modelVersion = "v2.0";
            var rawMetadata = "{\"steps\": 50, \"sampler\": \"euler_a\"}";

            // Act
            var historyRecord = await _orchestrator.LogGenerationAsync(
                ValidPngBytes,
                prompt,
                modelName,
                modelVersion,
                rawMetadata
            );

            // Track the created WEBP file path for teardown
            _createdWebpPath = historyRecord.ImagePath;

            // Assert
            historyRecord.Should().NotBeNull();
            historyRecord.Id.Should().BeGreaterThan(0);

            // 1. Verify file exists on disk
            File.Exists(historyRecord.ImagePath).Should().BeTrue();
            Path.GetExtension(historyRecord.ImagePath).Should().BeEquivalentTo(".webp");

            // Verify it is indeed a valid WEBP image by attempting to load it with ImageSharp
            using (var loadedImage = SixLabors.ImageSharp.Image.Load(historyRecord.ImagePath))
            {
                loadedImage.Width.Should().Be(1);
                loadedImage.Height.Should().Be(1);

                // Verify that EXIF and XMP metadata profiles are successfully embedded in the WebP image
                loadedImage.Metadata.ExifProfile.Should().NotBeNull();
                loadedImage.Metadata.XmpProfile.Should().NotBeNull();
            }

            // 2. Verify database insertion via SQLite connection
            using var connection = _databaseHelper.GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM GenerationHistory WHERE Id = @Id;";

            var param = cmd.CreateParameter();
            param.ParameterName = "@Id";
            param.Value = historyRecord.Id;
            cmd.Parameters.Add(param);

            using var reader = cmd.ExecuteReader();
            reader.Read().Should().BeTrue();
            reader["ImagePath"].ToString().Should().Be(historyRecord.ImagePath);
            reader["Prompt"].ToString().Should().Be(prompt);
            reader["ModelName"].ToString().Should().Be(modelName);
            reader["ModelVersion"].ToString().Should().Be(modelVersion);
            reader["RawMetadata"].ToString().Should().Be(rawMetadata);
        }

        [Fact]
        public async Task LogGenerationAsync_NullOrEmptyParameters_ThrowsExceptions()
        {
            // Assert Null Image Bytes
            Func<Task> act1 = async () => await _orchestrator.LogGenerationAsync(null!, "prompt", "model");
            await act1.Should().ThrowAsync<ArgumentException>().WithMessage("*Image bytes*");

            // Assert Empty Image Bytes
            Func<Task> act2 = async () => await _orchestrator.LogGenerationAsync(Array.Empty<byte>(), "prompt", "model");
            await act2.Should().ThrowAsync<ArgumentException>().WithMessage("*Image bytes*");

            // Assert Null Prompt
            Func<Task> act3 = async () => await _orchestrator.LogGenerationAsync(ValidPngBytes, null!, "model");
            await act3.Should().ThrowAsync<ArgumentException>().WithMessage("*Prompt*");

            // Assert Empty Prompt
            Func<Task> act4 = async () => await _orchestrator.LogGenerationAsync(ValidPngBytes, "  ", "model");
            await act4.Should().ThrowAsync<ArgumentException>().WithMessage("*Prompt*");

            // Assert Null ModelName
            Func<Task> act5 = async () => await _orchestrator.LogGenerationAsync(ValidPngBytes, "prompt", null!);
            await act5.Should().ThrowAsync<ArgumentException>().WithMessage("*Model name*");

            // Assert Empty ModelName
            Func<Task> act6 = async () => await _orchestrator.LogGenerationAsync(ValidPngBytes, "prompt", "   ");
            await act6.Should().ThrowAsync<ArgumentException>().WithMessage("*Model name*");
        }

    }
}