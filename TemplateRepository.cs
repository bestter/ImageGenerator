using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Repository class implementing async CRUD operations for AI prompt templates.
    /// Uses Dapper for high-performance micro-ORM mapping over SQLite.
    /// </summary>
    public class TemplateRepository
    {
        private readonly DatabaseHelper _databaseHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateRepository"/> class.
        /// </summary>
        /// <param name="databaseHelper">The database helper used to obtain connections.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="databaseHelper"/> is null.</exception>
        public TemplateRepository(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        /// <summary>
        /// Retrieves all templates from the database, ordered by creation date descending.
        /// </summary>
        /// <returns>A collection of all templates.</returns>
        public async Task<IEnumerable<TemplateModel>> GetAllAsync()
        {
            const string sql = "SELECT * FROM templates ORDER BY created_at DESC;";
            using var connection = _databaseHelper.GetConnection();
            return await connection.QueryAsync<TemplateModel>(sql);
        }

        /// <summary>
        /// Retrieves a single template by its unique key (case-insensitive).
        /// </summary>
        /// <param name="key">The unique key of the template.</param>
        /// <returns>The template if found; otherwise, null.</returns>
        public async Task<TemplateModel?> GetByKeyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            const string sql = "SELECT * FROM templates WHERE key = @Key LIMIT 1;";
            using var connection = _databaseHelper.GetConnection();
            return await connection.QueryFirstOrDefaultAsync<TemplateModel>(sql, new { Key = key });
        }

        /// <summary>
        /// Inserts a new template record.
        /// Sets the <see cref="TemplateModel.Id"/> property with the newly generated auto-incrementing value.
        /// </summary>
        /// <param name="template">The template model to insert.</param>
        /// <returns>The number of rows affected (should be 1 on success).</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="template"/> is null.</exception>
        public async Task<int> InsertAsync(TemplateModel template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            const string sql = @"
                INSERT INTO templates (key, value, category, tags, usage_count, last_used, created_at, updated_at)
                VALUES (@Key, @Value, @Category, @Tags, @UsageCount, @LastUsed, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();";

            using var connection = _databaseHelper.GetConnection();
            var id = await connection.ExecuteScalarAsync<long>(sql, template);
            template.Id = id;
            return 1;
        }

        /// <summary>
        /// Updates an existing template's key, value, category, tags, and usage statistics by its primary key ID.
        /// Automatically updates the <see cref="TemplateModel.UpdatedAt"/> timestamp to the current UTC time.
        /// </summary>
        /// <param name="template">The template model containing updated values.</param>
        /// <returns>True if the update succeeded and rows were affected; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="template"/> is null.</exception>
        public async Task<bool> UpdateAsync(TemplateModel template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            template.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE templates
                SET key = @Key,
                    value = @Value,
                    category = @Category,
                    tags = @Tags,
                    usage_count = @UsageCount,
                    last_used = @LastUsed,
                    updated_at = @UpdatedAt
                WHERE id = @Id;";

            using var connection = _databaseHelper.GetConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, template);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Deletes a template record by its unique key.
        /// </summary>
        /// <param name="key">The unique key of the template to delete.</param>
        /// <returns>True if the deletion succeeded and rows were affected; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            const string sql = "DELETE FROM templates WHERE key = @Key;";
            using var connection = _databaseHelper.GetConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Key = key });
            return rowsAffected > 0;
        }

        /// <summary>
        /// Increments the usage count of a template by 1 and updates the last used timestamp to UTC now.
        /// </summary>
        /// <param name="key">The unique key of the template.</param>
        /// <returns>True if the stats were successfully updated; otherwise, false.</returns>
        public async Task<bool> UpdateUsageStatsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            const string sql = @"
                UPDATE templates
                SET usage_count = usage_count + 1,
                    last_used = @LastUsed,
                    updated_at = @UpdatedAt
                WHERE key = @Key;";

            var now = DateTime.UtcNow;
            using var connection = _databaseHelper.GetConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Key = key, LastUsed = now, UpdatedAt = now });
            return rowsAffected > 0;
        }
    }
}
