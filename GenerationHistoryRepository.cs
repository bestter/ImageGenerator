using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Repository class implementing async database operations for AI image generation history.
    /// Uses Dapper for high-performance micro-ORM mapping over SQLite.
    /// </summary>
    public class GenerationHistoryRepository
    {
        private readonly DatabaseHelper _databaseHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationHistoryRepository"/> class.
        /// </summary>
        /// <param name="databaseHelper">The database helper used to obtain connections.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="databaseHelper"/> is null.</exception>
        public GenerationHistoryRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        /// <summary>
        /// Inserts a new generation history entry into the SQLite database.
        /// Sets the <see cref="GenerationHistoryModel.Id"/> property with the newly generated auto-incrementing value.
        /// </summary>
        /// <param name="history">The generation history entry to insert.</param>
        /// <returns>The newly generated primary key Id as an integer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="history"/> is null.</exception>
        public async Task<int> InsertAsync(GenerationHistoryModel history)
        {
            if (history == null)
            {
                throw new ArgumentNullException(nameof(history));
            }

            const string sql = @"
                INSERT INTO GenerationHistory (ImagePath, Prompt, ModelName, ModelVersion, RawMetadata, CreatedAt)
                VALUES (@ImagePath, @Prompt, @ModelName, @ModelVersion, @RawMetadata, @CreatedAt);
                SELECT last_insert_rowid();";

            using var connection = _databaseHelper.GetConnection();
            var id = await connection.ExecuteScalarAsync<long>(sql, history);
            history.Id = id;
            return (int)id;
        }

        /// <summary>
        /// Retrieves all generation history records, ordered by creation date descending.
        /// </summary>
        /// <returns>A collection of all history records.</returns>
        public async Task<IEnumerable<GenerationHistoryModel>> GetAllAsync()
        {
            // ⚡ Bolt Optimization: Avoid fetching full entity models when only keys/summaries are needed.
            // Omitting the large RawMetadata column drastically reduces LOH allocations and I/O bottlenecks during list population.
            const string sql = "SELECT Id, ImagePath, Prompt, ModelName, ModelVersion, CreatedAt FROM GenerationHistory ORDER BY CreatedAt DESC;";
            using var connection = _databaseHelper.GetConnection();
            return await connection.QueryAsync<GenerationHistoryModel>(sql);
        }

        /// <summary>
        /// Searches generation history records by matching a search term inside the Prompt or ModelName.
        /// Results are ordered by creation date descending.
        /// </summary>
        /// <param name="searchTerm">The string keyword to search for.</param>
        /// <returns>A filtered collection of matching history records.</returns>
        public async Task<IEnumerable<GenerationHistoryModel>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAsync();
            }

            // ⚡ Bolt Optimization: Avoid fetching full entity models when only keys/summaries are needed.
            const string sql = @"
                SELECT Id, ImagePath, Prompt, ModelName, ModelVersion, CreatedAt FROM GenerationHistory
                WHERE Prompt LIKE '%' || @Query || '%' ESCAPE '\'
                   OR ModelName LIKE '%' || @Query || '%' ESCAPE '\'
                ORDER BY CreatedAt DESC;";

            // SÉCURITÉ : Échappe les caractères joker SQL (%, _, [, et le caractère d'échappement lui-même)
            // pour prévenir les attaques par injection de wildcards (qui peuvent causer des lenteurs DoS).
            var escapedTerm = searchTerm.Trim()
                .Replace(@"\", @"\\")
                .Replace("%", @"\%")
                .Replace("_", @"\_")
                .Replace("[", @"\[");

            using var connection = _databaseHelper.GetConnection();
            return await connection.QueryAsync<GenerationHistoryModel>(sql, new { Query = escapedTerm });
        }

        /// <summary>
        /// Retrieves only the RawMetadata column for a specific generation history record.
        /// </summary>
        /// <param name="id">The Id of the history record.</param>
        /// <returns>The raw metadata JSON string, or null if not found.</returns>
        public async Task<string?> GetRawMetadataAsync(int id)
        {
            const string sql = "SELECT RawMetadata FROM GenerationHistory WHERE Id = @Id;";
            using var connection = _databaseHelper.GetConnection();
            return await connection.QueryFirstOrDefaultAsync<string>(sql, new { Id = id });
        }
    }
}