using System;
using System.IO;
using Dapper;

namespace ImageGeneratorApp.Tests
{
    public class DatabaseHelperTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public DatabaseHelperTests()
        {
            // Use a physical temporary file so the tests closely match real usage
            _dbPath = Path.Combine(Path.GetTempPath(), $"ImageGenerator_DBHelperTest_{Guid.NewGuid()}.db");
            _connectionString = $"Data Source={_dbPath}";
        }

        public void Dispose()
        {
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
                    // Ignore deletion failures during cleanup
                }
            }
        }

        [Fact]
        public void InitializeDatabase_ShouldCreateRequiredTablesAndIndexes()
        {
            // Arrange
            var dbHelper = new DatabaseHelper(_connectionString);

            // Act
            dbHelper.InitializeDatabase();

            // Assert
            using var connection = dbHelper.GetConnection();
            connection.Open();

            var tables = connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table';").AsList();
            tables.Should().Contain("templates");
            tables.Should().Contain("GenerationHistory");

            var indexes = connection.Query<string>("SELECT name FROM sqlite_master WHERE type='index';").AsList();
            indexes.Should().Contain("IX_templates_key");
            indexes.Should().Contain("IX_templates_category");
        }
    }
}
