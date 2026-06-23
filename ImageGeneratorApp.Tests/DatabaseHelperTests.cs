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

        [Fact]
        public void GetConnection_ReturnsNewConnectionWithCorrectConnectionString()
        {
            // Arrange
            var customConnectionString = $"Data Source={_customDbPath}";
            var helper = new DatabaseHelper(customConnectionString);

            // Act
            using var connection = helper.GetConnection();

            // Assert
            connection.Should().NotBeNull();
            connection.ConnectionString.Should().Be(customConnectionString);
            connection.State.Should().Be(System.Data.ConnectionState.Closed, "because the connection should not be opened automatically");
        }

        [Fact]
        public void GetConnection_CalledMultipleTimes_ReturnsDifferentInstances()
        {
            // Arrange
            var customConnectionString = $"Data Source={_customDbPath}";
            var helper = new DatabaseHelper(customConnectionString);

            // Act
            using var connection1 = helper.GetConnection();
            using var connection2 = helper.GetConnection();

            // Assert
            connection1.Should().NotBeNull();
            connection2.Should().NotBeNull();
            connection1.Should().NotBeSameAs(connection2, "because GetConnection should return a new instance each time");
        }


        [Fact]
        public void InitializeDatabase_CreatesRequiredTables()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"ImageGenerator_DbHelperTest_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var helper = new DatabaseHelper(connectionString);

            // Act
            helper.InitializeDatabase();

            // Assert
            using var connection = helper.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name IN ('templates', 'GenerationHistory');";
            var tableCount = (long)command.ExecuteScalar()!;
            tableCount.Should().Be(2, "because InitializeDatabase should create both 'templates' and 'GenerationHistory' tables");

            // Cleanup for this test
            connection.Close();
            SqliteConnection.ClearPool(connection);
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        [Fact]
        public void InitializeDatabase_CreatesRequiredIndexes()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"ImageGenerator_DbHelperTest_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var helper = new DatabaseHelper(connectionString);

            // Act
            helper.InitializeDatabase();

            // Assert
            using var connection = helper.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='index' AND name IN ('IX_templates_key', 'IX_templates_category', 'IX_templates_created_at', 'IX_GenerationHistory_CreatedAt');";
            using var reader = command.ExecuteReader();

            var indexNames = new System.Collections.Generic.List<string>();
            while (reader.Read())
            {
                indexNames.Add(reader.GetString(0));
            }

            indexNames.Should().Contain("IX_templates_key");
            indexNames.Should().Contain("IX_templates_category");
            indexNames.Should().Contain("IX_templates_created_at");
            indexNames.Should().Contain("IX_GenerationHistory_CreatedAt");
            indexNames.Count.Should().Be(4, "because 4 standard indexes should be created");

            // Cleanup for this test
            connection.Close();
            SqliteConnection.ClearPool(connection);
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        [Fact]
        public void InitializeDatabase_IsIdempotent_CanBeCalledMultipleTimesWithoutError()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"ImageGenerator_DbHelperTest_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var helper = new DatabaseHelper(connectionString);

            // Act
            Action act = () =>
            {
                helper.InitializeDatabase();
                helper.InitializeDatabase();
                helper.InitializeDatabase();
            };

            // Assert
            act.Should().NotThrow("because InitializeDatabase uses IF NOT EXISTS and should be safely idempotent");

            // Cleanup for this test
            using var connection = helper.GetConnection();
            SqliteConnection.ClearPool(connection);
            if (File.Exists(dbPath)) File.Delete(dbPath);
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
