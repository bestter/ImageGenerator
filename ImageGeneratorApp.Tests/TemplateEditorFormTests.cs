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

using ImageGeneratorApp;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageGeneratorApp.Tests
{
    public class TemplateEditorFormTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly DatabaseHelper _dbHelper;
        private readonly TemplateRepository _repository;

        public TemplateEditorFormTests()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.sqlite");
            var connectionString = $"Data Source={_tempDbPath}";
            _dbHelper = new DatabaseHelper(connectionString);
            _dbHelper.InitializeDatabase();
            _repository = new TemplateRepository(_dbHelper);
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            Action act = () => new TemplateEditorForm(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
        }

        [Fact]
        public void Constructor_AddMode_InitializesCorrectly()
        {
            using var form = new TemplateEditorForm(_repository);

            form.Text.Should().Be("Ajouter un modèle de prompt");
            form.Controls.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Constructor_EditMode_InitializesCorrectly_AndPopulatesFields()
        {
            var template = new TemplateModel
            {
                Id = 1,
                Key = "test_key",
                Category = "Test Category",
                Tags = "tag1, tag2",
                Value = "Test Value"
            };

            using var form = new TemplateEditorForm(_repository, template);

            form.Text.Should().Be("Modifier le modèle de prompt");
            form.Controls.Count.Should().BeGreaterThan(0);

            // Reflection to access private fields to verify they are populated correctly
            var type = form.GetType();

            var txtKey = (TextBox)type.GetField("txtKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(form)!;
            var txtCategory = (TextBox)type.GetField("txtCategory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(form)!;
            var txtTags = (TextBox)type.GetField("txtTags", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(form)!;
            var txtValue = (TextBox)type.GetField("txtValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(form)!;

            txtKey.Text.Should().Be("test_key");
            txtCategory.Text.Should().Be("Test Category");
            txtTags.Text.Should().Be("tag1, tag2");
            txtValue.Text.Should().Be("Test Value");
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