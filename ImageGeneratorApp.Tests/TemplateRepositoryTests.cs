// AI Image generator. A program to generate image from AI API.
// Copyright (C) 2026  Martin Labelle
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using FluentAssertions;
using ImageGeneratorApp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    /// <summary>
    /// Integration/Unit tests for <see cref="TemplateRepository"/> and <see cref="DatabaseHelper"/>.
    /// Uses a temporary on-disk SQLite database file per test to guarantee a realistic environment and handles clean up in teardown.
    /// </summary>
    public class TemplateRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly DatabaseHelper _databaseHelper;
        private readonly TemplateRepository _repository;

        public TemplateRepositoryTests()
        {
            // Create a unique temporary SQLite file for isolated test execution
            _dbPath = Path.Combine(Path.GetTempPath(), $"ImageGenerator_Test_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={_dbPath}";

            // Initialize helper and repository
            _databaseHelper = new DatabaseHelper(connectionString);
            _repository = new TemplateRepository(_databaseHelper);
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
        public async Task InsertAsync_ShouldSaveTemplateAndSetId()
        {
            // Arrange
            var template = new TemplateModel
            {
                Key = "cinematic_retro",
                Value = "Cinematic retro style, 35mm film photograph, highly detailed",
                Category = "Style",
                Tags = "cinematic,retro,film"
            };

            // Act
            int affectedRows = await _repository.InsertAsync(template);
            var retrieved = await _repository.GetByKeyAsync("cinematic_retro");

            // Assert
            affectedRows.Should().Be(1);
            template.Id.Should().BeGreaterThan(0);
            retrieved.Should().NotBeNull();
            retrieved!.Key.Should().Be("cinematic_retro");
            retrieved.Value.Should().Be(template.Value);
            retrieved.Category.Should().Be(template.Category);
            retrieved.Tags.Should().Be(template.Tags);
            retrieved.UsageCount.Should().Be(0);
            retrieved.LastUsed.Should().BeNull();
        }

        [Fact]
        public async Task GetByKeyAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            var template = new TemplateModel
            {
                Key = "CyberPunk_Green",
                Value = "Neon green cyberpunk aesthetic",
                Category = "Sci-Fi"
            };
            await _repository.InsertAsync(template);

            // Act
            var retrievedLower = await _repository.GetByKeyAsync("cyberpunk_green");
            var retrievedUpper = await _repository.GetByKeyAsync("CYBERPUNK_GREEN");

            // Assert
            retrievedLower.Should().NotBeNull();
            retrievedUpper.Should().NotBeNull();
            retrievedLower!.Id.Should().Be(template.Id);
            retrievedUpper!.Id.Should().Be(template.Id);
        }

        [Fact]
        public async Task GetByKeysAsync_WithNullEmptyOrWhitespaceKeys_ShouldReturnEmptyArray()
        {
            // Act & Assert for null
            var resultNull = await _repository.GetByKeysAsync(null!);
            resultNull.Should().BeEmpty();

            // Act & Assert for empty
            var resultEmpty = await _repository.GetByKeysAsync(Array.Empty<string>());
            resultEmpty.Should().BeEmpty();

            // Act & Assert for whitespace only
            var resultWhitespace = await _repository.GetByKeysAsync(new[] { " ", "", null! });
            resultWhitespace.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTemplatesOrderedByCreatedAtDescending()
        {
            // Arrange
            var t1 = new TemplateModel { Key = "key1", Value = "val1", CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
            var t2 = new TemplateModel { Key = "key2", Value = "val2", CreatedAt = DateTime.UtcNow };
            await _repository.InsertAsync(t1);
            await _repository.InsertAsync(t2);

            // Act
            var templates = (await _repository.GetAllAsync()).ToList();

            // Assert
            templates.Should().HaveCount(2);
            templates[0].Key.Should().Be("key2"); // most recent first
            templates[1].Key.Should().Be("key1");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyTemplateAndSetUpdatedAt()
        {
            // Arrange
            var template = new TemplateModel { Key = "original", Value = "original val" };
            await _repository.InsertAsync(template);
            var originalUpdatedAt = template.UpdatedAt;

            // Wait a brief moment to ensure time progression
            await Task.Delay(15);

            // Act
            template.Key = "renamed";
            template.Value = "updated val";
            template.Category = "New Category";
            bool success = await _repository.UpdateAsync(template);
            var retrievedOld = await _repository.GetByKeyAsync("original");
            var retrievedNew = await _repository.GetByKeyAsync("renamed");

            // Assert
            success.Should().BeTrue();
            retrievedOld.Should().BeNull();
            retrievedNew.Should().NotBeNull();
            retrievedNew!.Value.Should().Be("updated val");
            retrievedNew.Category.Should().Be("New Category");
            retrievedNew.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveTemplate()
        {
            // Arrange
            var template = new TemplateModel { Key = "to_delete", Value = "delete me" };
            await _repository.InsertAsync(template);

            // Act
            bool deleted = await _repository.DeleteAsync("to_delete");
            var retrieved = await _repository.GetByKeyAsync("to_delete");

            // Assert
            deleted.Should().BeTrue();
            retrieved.Should().BeNull();
        }

        [Fact]
        public async Task UpdateUsageStatsAsync_ShouldIncrementUsageCountAndSetLastUsed()
        {
            // Arrange
            var template = new TemplateModel { Key = "stat_test", Value = "test prompt" };
            await _repository.InsertAsync(template);

            // Act
            bool success1 = await _repository.UpdateUsageStatsAsync("stat_test");
            var afterFirst = await _repository.GetByKeyAsync("stat_test");

            bool success2 = await _repository.UpdateUsageStatsAsync("stat_test");
            var afterSecond = await _repository.GetByKeyAsync("stat_test");

            // Assert
            success1.Should().BeTrue();
            afterFirst.Should().NotBeNull();
            afterFirst!.UsageCount.Should().Be(1);
            afterFirst.LastUsed.Should().NotBeNull();

            success2.Should().BeTrue();
            afterSecond.Should().NotBeNull();
            afterSecond!.UsageCount.Should().Be(2);
        }
    }
}