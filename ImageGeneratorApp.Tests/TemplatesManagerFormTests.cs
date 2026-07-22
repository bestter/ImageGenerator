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
    public class TemplatesManagerFormTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly DatabaseHelper _dbHelper;
        private readonly TemplateRepository _repository;

        public TemplatesManagerFormTests()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_tmpl_mgr_{Guid.NewGuid()}.sqlite");
            var connectionString = $"Data Source={_tempDbPath}";
            _dbHelper = new DatabaseHelper(connectionString);
            _dbHelper.InitializeDatabase();
            _repository = new TemplateRepository(_dbHelper);
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            Action act = () => new TemplatesManagerForm(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
        }

        [Fact]
        public void Constructor_ValidRepository_InitializesControlsAndProperties()
        {
            using var form = new TemplatesManagerForm(_repository);

            form.Text.Should().Be("Gestionnaire de gabarits de prompt");
            form.Size.Should().Be(new Size(900, 550));
            form.MinimumSize.Should().Be(new Size(700, 400));
            form.StartPosition.Should().Be(FormStartPosition.CenterParent);
            form.Controls.Count.Should().BeGreaterThan(0);
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
