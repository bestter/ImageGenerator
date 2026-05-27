using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Helper class responsible for initializing the SQLite database and providing connections.
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        static DatabaseHelper()
        {
            // Enable Dapper snake_case to PascalCase property mapping (e.g., usage_count to UsageCount)
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHelper"/> class.
        /// </summary>
        /// <param name="connectionString">
        /// Optional connection string. If null, the database file 'templates.db' 
        /// will be created in Environment.SpecialFolder.LocalApplicationData.
        /// </param>
        public DatabaseHelper(string? connectionString = null)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                _connectionString = connectionString;
            }
            else
            {
                var appDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ImageGeneratorApp"
                );

                // Ensure the directory exists
                Directory.CreateDirectory(appDataFolder);

                var dbPath = Path.Combine(appDataFolder, "templates.db");
                _connectionString = $"Data Source={dbPath}";
            }

            // Automatically ensure the database and table exist
            InitializeDatabase();
        }

        /// <summary>
        /// Gets the connection string used by the helper.
        /// </summary>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// Creates and returns a new <see cref="SqliteConnection"/> using the connection string.
        /// Note: The caller is responsible for opening and disposing the connection.
        /// </summary>
        /// <returns>A SQLite database connection instance.</returns>
        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// Ensures that the database schema is correctly initialized, creating the tables and indexes if they do not exist.
        /// </summary>
        public void InitializeDatabase()
        {
            using var connection = GetConnection();
            connection.Open();

            // Create templates table if not exists
            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS templates (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    key TEXT UNIQUE NOT NULL COLLATE NOCASE,
                    value TEXT NOT NULL,
                    category TEXT,
                    tags TEXT,
                    usage_count INTEGER DEFAULT 0,
                    last_used DATETIME,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                );";

            connection.Execute(createTableSql);

            // Create GenerationHistory table if not exists
            const string createHistoryTableSql = @"
                CREATE TABLE IF NOT EXISTS GenerationHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ImagePath TEXT NOT NULL,
                    Prompt TEXT NOT NULL,
                    ModelName TEXT NOT NULL,
                    ModelVersion TEXT,
                    RawMetadata TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";

            connection.Execute(createHistoryTableSql);

            // Create standard indexes for efficient lookup
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_templates_key ON templates(key);");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_templates_category ON templates(category);");
        }
    }
}