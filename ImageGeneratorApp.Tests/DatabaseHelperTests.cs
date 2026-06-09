using FluentAssertions;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class DatabaseHelperTests : IDisposable
    {
        private readonly string _customDbPath;

        public DatabaseHelperTests()
        {
            _customDbPath = Path.Combine(Path.GetTempPath(), $"ImageGenerator_DbHelperTest_{Guid.NewGuid()}.db");
        }

        [Fact]
        public void Constructor_WithoutConnectionString_SetsDefaultConnectionStringAndCreatesFile()
        {
            // Arrange
            var expectedAppDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImageGeneratorApp"
            );
            var expectedDbPath = Path.Combine(expectedAppDataFolder, "templates.db");

            // Act
            var helper = new DatabaseHelper();

            // Assert
            helper.ConnectionString.Should().Be($"Data Source={expectedDbPath}");
            File.Exists(expectedDbPath).Should().BeTrue("because the database file should be created automatically during initialization");

            // Verify we can connect and the database initialized correctly
            using var connection = helper.GetConnection();
            connection.Open();
            connection.State.Should().Be(System.Data.ConnectionState.Open);

            // Note: We don't delete this default DB since it might be the user's actual dev database.
            // The table creation inside InitializeDatabase uses 'IF NOT EXISTS', so it's safe to run.
        }

        [Fact]
        public void Constructor_WithCustomConnectionString_SetsConnectionStringAndCreatesFile()
        {
            // Arrange
            var customConnectionString = $"Data Source={_customDbPath}";

            // Act
            var helper = new DatabaseHelper(customConnectionString);

            // Assert
            helper.ConnectionString.Should().Be(customConnectionString);
            File.Exists(_customDbPath).Should().BeTrue("because the database file should be created automatically during initialization for a custom path");

            // Verify tables were created
            using var connection = helper.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name IN ('templates', 'GenerationHistory');";
            var tableCount = (long)command.ExecuteScalar()!;
            tableCount.Should().Be(2, "because InitializeDatabase should create both 'templates' and 'GenerationHistory' tables");
        }

        public void Dispose()
        {
            // Teardown: ensure the database connection is fully closed so the file is freed, then delete it
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (File.Exists(_customDbPath))
            {
                try
                {
                    File.Delete(_customDbPath);
                }
                catch
                {
                    // Ignore exceptions during cleanup in tests
                }
            }
        }
    }
}
