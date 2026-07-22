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
            form.PromptToLoad.Should().BeNull();
            form.ModelToLoad.Should().BeNull();
        }

        [Fact]
        public void GenerationHistoryModel_CreatedAtLocal_ConvertsUtcToLocalTime()
        {
            var utcTime = new DateTime(2026, 7, 22, 14, 30, 0, DateTimeKind.Utc);
            var model = new GenerationHistoryModel
            {
                CreatedAt = utcTime
            };

            var expectedLocalTime = utcTime.ToLocalTime();
            model.CreatedAtLocal.Should().Be(expectedLocalTime);
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
    }
}
