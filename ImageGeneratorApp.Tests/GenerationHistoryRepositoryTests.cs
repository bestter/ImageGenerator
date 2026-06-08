using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    /// <summary>
    /// Integration tests for <see cref="GenerationHistoryRepository"/> and <see cref="DatabaseHelper"/>.
    /// Uses a temporary on-disk SQLite database file per test to guarantee a realistic environment and handles clean up in teardown.
    /// </summary>
    public class GenerationHistoryRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly DatabaseHelper _databaseHelper;
        private readonly GenerationHistoryRepository _repository;

        public GenerationHistoryRepositoryTests()
        {
            // Create a unique temporary SQLite file for isolated test execution
            _dbPath = Path.Combine(Path.GetTempPath(), $"ImageGenerator_HistoryTest_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={_dbPath}";

            // Initialize helper and repository (InitializeDatabase is called internally on construction)
            _databaseHelper = new DatabaseHelper(connectionString);
            _repository = new GenerationHistoryRepository(_databaseHelper);
        }

        public void Dispose()
        {
            // Teardown: ensure the database connection is fully closed so the file is freed, then delete it
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (File.Exists(_dbPath))
            {
                try
                {
                    File.Delete(_dbPath);
                }
                catch
                {
                    // Ignore deletion failures during test cleanup
                }
            }
        }

        [Fact]
        public async Task InsertAsync_ShouldSaveGenerationHistoryAndReturnId()
        {
            // Arrange
            var history = new GenerationHistoryModel
            {
                ImagePath = @"C:\Exports\image_123.png",
                Prompt = "A modern abstract painting, oil on canvas, highly textured",
                ModelName = "grok-imagine-image-pro",
                ModelVersion = "1.5.pro",
                RawMetadata = "{\"seed\": 42, \"cfg_scale\": 7.5}",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            int generatedId = await _repository.InsertAsync(history);

            // Assert
            generatedId.Should().BeGreaterThan(0);
            history.Id.Should().Be(generatedId);

            // Directly query SQLite to verify properties were inserted and retrieved correctly
            using var connection = _databaseHelper.GetConnection();
            connection.Open();

            // Using raw SQL query with Dapper to verify mapping of our model
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM GenerationHistory WHERE Id = @Id;";

            var param = cmd.CreateParameter();
            param.ParameterName = "@Id";
            param.Value = generatedId;
            cmd.Parameters.Add(param);

            using var reader = cmd.ExecuteReader();
            reader.Read().Should().BeTrue();
            reader["ImagePath"].ToString().Should().Be(history.ImagePath);
            reader["Prompt"].ToString().Should().Be(history.Prompt);
            reader["ModelName"].ToString().Should().Be(history.ModelName);
            reader["ModelVersion"].ToString().Should().Be(history.ModelVersion);
            reader["RawMetadata"].ToString().Should().Be(history.RawMetadata);

            var dbCreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
            // Datetime precision might differ slightly due to SQLite/C# conversions, check within a 1-second delta
            dbCreatedAt.Should().BeCloseTo(history.CreatedAt, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task InsertAsync_NullHistory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Func<Task> act = async () => await _repository.InsertAsync(null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllRecordsOrderedByCreatedAtDescending()
        {
            // Arrange
            var record1 = new GenerationHistoryModel
            {
                ImagePath = "path1.webp",
                Prompt = "First prompt",
                ModelName = "model1",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            var record2 = new GenerationHistoryModel
            {
                ImagePath = "path2.webp",
                Prompt = "Second prompt",
                ModelName = "model2",
                CreatedAt = DateTime.UtcNow // newer
            };

            await _repository.InsertAsync(record1);
            await _repository.InsertAsync(record2);

            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            results.Should().NotBeNull();
            var list = System.Linq.Enumerable.ToList(results);
            list.Count.Should().Be(2);
            list[0].ImagePath.Should().Be("path2.webp"); // newer should be first
            list[0].RawMetadata.Should().BeNull(); // ⚡ Bolt Optimization: RawMetadata omitted from GetAll
            list[1].ImagePath.Should().Be("path1.webp");
        }

        [Fact]
        public async Task SearchAsync_ShouldFilterByPromptOrModelName()
        {
            // Arrange
            var r1 = new GenerationHistoryModel { ImagePath = "p1.webp", Prompt = "A cute cat playing with yarn", ModelName = "grok-imagine" };
            var r2 = new GenerationHistoryModel { ImagePath = "p2.webp", Prompt = "A futuristic city in the desert", ModelName = "nano-banana" };
            var r3 = new GenerationHistoryModel { ImagePath = "p3.webp", Prompt = "A standard dog sleeping", ModelName = "grok-pro" };

            await _repository.InsertAsync(r1);
            await _repository.InsertAsync(r2);
            await _repository.InsertAsync(r3);

            // Act & Assert case-insensitive matching in Prompt
            var searchYarn = await _repository.SearchAsync("YARN");
            searchYarn.Should().ContainSingle().Which.ImagePath.Should().Be("p1.webp");

            // Act & Assert case-insensitive matching in ModelName
            var searchNano = await _repository.SearchAsync("nano");
            searchNano.Should().ContainSingle().Which.ImagePath.Should().Be("p2.webp");

            // Act & Assert multiple matches (grok matches r1 and r3)
            var searchGrok = await _repository.SearchAsync("grok");
            var grokList = System.Linq.Enumerable.ToList(searchGrok);
            grokList.Count.Should().Be(2);
            grokList[0].RawMetadata.Should().BeNull(); // ⚡ Bolt Optimization: RawMetadata omitted from Search

            // Act & Assert empty/null query returns all
            var searchEmpty = await _repository.SearchAsync("  ");
            System.Linq.Enumerable.Count(searchEmpty).Should().Be(3);
        }

        [Fact]
        public async Task GetRawMetadataAsync_ShouldReturnMetadata_WhenIdExists()
        {
            // Arrange
            var history = new GenerationHistoryModel
            {
                ImagePath = "path.webp",
                Prompt = "Some prompt",
                ModelName = "model",
                RawMetadata = "{\"key\": \"value\"}",
                CreatedAt = DateTime.UtcNow
            };
            int generatedId = await _repository.InsertAsync(history);

            // Act
            var metadata = await _repository.GetRawMetadataAsync(generatedId);

            // Assert
            metadata.Should().Be("{\"key\": \"value\"}");
        }

        [Fact]
        public async Task GetRawMetadataAsync_ShouldReturnNull_WhenIdDoesNotExist()
        {
            // Act
            var metadata = await _repository.GetRawMetadataAsync(9999);

            // Assert
            metadata.Should().BeNull();
        }
    }
}