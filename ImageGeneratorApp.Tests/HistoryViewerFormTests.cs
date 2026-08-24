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
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class HistoryViewerFormTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly DatabaseHelper _dbHelper;
        private readonly GenerationHistoryRepository _repository;
        private readonly ImageProcessingService _imageProcessingService;

        public HistoryViewerFormTests()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_hist_view_{Guid.NewGuid()}.sqlite");
            var connectionString = $"Data Source={_tempDbPath}";
            _dbHelper = new DatabaseHelper(connectionString);
            _dbHelper.InitializeDatabase();
            _repository = new GenerationHistoryRepository(_dbHelper);
            _imageProcessingService = new ImageProcessingService();
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            Action act = () => new HistoryViewerForm(null!, _imageProcessingService);
            act.Should().Throw<ArgumentNullException>().WithParameterName("historyRepository");
        }

        [Fact]
        public void Constructor_WithNullImageProcessingService_ThrowsArgumentNullException()
        {
            Action act = () => new HistoryViewerForm(_repository, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("imageProcessingService");
        }

        [Fact]
        public void Constructor_ValidDependencies_InitializesControlsAndProperties()
        {
            using var form = new HistoryViewerForm(_repository, _imageProcessingService);

            form.Text.Should().Be("Historique des Générations");
            form.Size.Should().Be(new Size(1100, 700));
            form.MinimumSize.Should().Be(new Size(800, 500));
            form.StartPosition.Should().Be(FormStartPosition.CenterParent);
            form.Controls.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task SelectingARow_LoadsRawMetadataOntoTheBoundModel()
        {
            var originalMetadata = "{\"seed\": 1}";
            await InsertHistoryAsync("first prompt", "model-a", originalMetadata, DateTime.UtcNow);

            using var form = CreateLoadedForm();
            var grid = FindNamed<DataGridView>(form, "dataGridViewHistory");
            var txtMetadata = FindNamed<TextBox>(form, "txtMetadata");

            await WaitUntilAsync(() => grid.Rows.Count == 1);
            SelectRow(grid, 0);

            await WaitUntilAsync(() => BoundHistory(grid.Rows[0]).RawMetadata != null);

            BoundHistory(grid.Rows[0]).RawMetadata.Should().Be(originalMetadata);
            txtMetadata.Text.Should().Contain("\"seed\"");
        }

        [Fact]
        public async Task SelectingTheSameRowAgain_DoesNotRefetchRawMetadataFromTheDatabase()
        {
            var originalMetadata = "{\"seed\": 11}";
            var older = await InsertHistoryAsync("older prompt", "model-a", originalMetadata, DateTime.UtcNow.AddMinutes(-1));
            await InsertHistoryAsync("newer prompt", "model-b", "{\"seed\": 22}", DateTime.UtcNow);

            using var form = CreateLoadedForm();
            var grid = FindNamed<DataGridView>(form, "dataGridViewHistory");

            await WaitUntilAsync(() => grid.Rows.Count == 2);

            int olderRow = FindRowIndex(grid, older.Id);
            SelectRow(grid, olderRow);
            await WaitUntilAsync(() => BoundHistory(grid.Rows[olderRow]).RawMetadata != null);
            BoundHistory(grid.Rows[olderRow]).RawMetadata.Should().Be(originalMetadata);

            UpdateRawMetadataInDatabase(older.Id, "{\"seed\": 99}");

            int newerRow = olderRow == 0 ? 1 : 0;
            SelectRow(grid, newerRow);
            await WaitUntilAsync(() => BoundHistory(grid.Rows[newerRow]).RawMetadata != null);

            SelectRow(grid, olderRow);
            await WaitUntilAsync(() => BoundHistory(grid.Rows[olderRow]).RawMetadata != null);

            BoundHistory(grid.Rows[olderRow]).RawMetadata.Should().Be(originalMetadata);
        }

        [Fact]
        public async Task SelectingARowWithNullRawMetadata_CachesEmptyStringAndShowsFallbackText()
        {
            await InsertHistoryAsync("empty meta", "model-a", null, DateTime.UtcNow);

            using var form = CreateLoadedForm();
            var grid = FindNamed<DataGridView>(form, "dataGridViewHistory");
            var txtMetadata = FindNamed<TextBox>(form, "txtMetadata");

            await WaitUntilAsync(() => grid.Rows.Count == 1);
            SelectRow(grid, 0);
            await WaitUntilAsync(() => BoundHistory(grid.Rows[0]).RawMetadata != null);

            BoundHistory(grid.Rows[0]).RawMetadata.Should().Be(string.Empty);
            txtMetadata.Text.Should().Be("Aucune métadonnée disponible.");
        }

        public void Dispose()
        {
            if (File.Exists(_tempDbPath))
            {
                try
                {
                    File.Delete(_tempDbPath);
                }
                catch { }
            }
        }

        private HistoryViewerForm CreateLoadedForm()
        {
            var form = new HistoryViewerForm(_repository, _imageProcessingService);
            _ = form.Handle;
            InvokeOnLoad(form);
            return form;
        }

        private async Task<GenerationHistoryModel> InsertHistoryAsync(
            string prompt,
            string modelName,
            string? rawMetadata,
            DateTime createdAt)
        {
            var history = new GenerationHistoryModel
            {
                ImagePath = string.Empty,
                Prompt = prompt,
                ModelName = modelName,
                RawMetadata = rawMetadata,
                CreatedAt = createdAt
            };
            await _repository.InsertAsync(history);
            return history;
        }

        private void UpdateRawMetadataInDatabase(long id, string rawMetadata)
        {
            using var connection = _dbHelper.GetConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE GenerationHistory SET RawMetadata = @metadata WHERE Id = @id;";
            var metadataParam = cmd.CreateParameter();
            metadataParam.ParameterName = "@metadata";
            metadataParam.Value = rawMetadata;
            cmd.Parameters.Add(metadataParam);
            var idParam = cmd.CreateParameter();
            idParam.ParameterName = "@id";
            idParam.Value = id;
            cmd.Parameters.Add(idParam);
            cmd.ExecuteNonQuery().Should().Be(1);
        }

        private static void InvokeOnLoad(Form form)
        {
            var onLoad = typeof(HistoryViewerForm).GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic);
            onLoad.Should().NotBeNull();
            onLoad!.Invoke(form, new object[] { EventArgs.Empty });
        }

        private static T FindNamed<T>(Control root, string name) where T : Control
        {
            Control[] found = root.Controls.Find(name, true);
            found.Should().ContainSingle();
            found[0].Should().BeOfType<T>();
            return (T)found[0];
        }

        private static void SelectRow(DataGridView grid, int rowIndex)
        {
            grid.ClearSelection();
            DataGridViewRow row = grid.Rows[rowIndex];
            if (row.Cells.Count > 0)
            {
                grid.CurrentCell = row.Cells[0];
            }

            row.Selected = true;
        }

        private static int FindRowIndex(DataGridView grid, long historyId)
        {
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                if (BoundHistory(grid.Rows[i]).Id == historyId)
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"History id {historyId} was not found in the grid.");
        }

        private static GenerationHistoryModel BoundHistory(DataGridViewRow row)
        {
            row.DataBoundItem.Should().BeOfType<GenerationHistoryModel>();
            return (GenerationHistoryModel)row.DataBoundItem;
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            var start = DateTime.UtcNow;
            while (!condition() && DateTime.UtcNow - start < TimeSpan.FromSeconds(5))
            {
                Application.DoEvents();
                await Task.Delay(10);
            }

            condition().Should().BeTrue("the UI did not reach the expected state within 5 seconds");
        }
    }
}