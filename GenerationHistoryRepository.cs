using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

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
            const string sql = "SELECT * FROM GenerationHistory ORDER BY CreatedAt DESC;";
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

            const string sql = @"
                SELECT * FROM GenerationHistory 
                WHERE Prompt LIKE @Query 
                   OR ModelName LIKE @Query 
                ORDER BY CreatedAt DESC;";

            var queryParam = $"%{searchTerm.Trim()}%";
            using var connection = _databaseHelper.GetConnection();
            return await connection.QueryAsync<GenerationHistoryModel>(sql, new { Query = queryParam });
        }
    }
}
