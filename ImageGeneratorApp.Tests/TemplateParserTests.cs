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
using System.Threading.Tasks;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    /// <summary>
    /// Integration/Unit tests for <see cref="TemplateParser"/>.
    /// Uses an isolated, temporary SQLite database to seed templates before parsing.
    /// </summary>
    public class TemplateParserTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly DatabaseHelper _databaseHelper;
        private readonly TemplateRepository _repository;
        private readonly TemplateParser _parser;

        public TemplateParserTests()
        {
            // Set up a temporary unique SQLite database for test runs
            _dbPath = Path.Combine(Path.GetTempPath(), $"ImageGenerator_ParserTest_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={_dbPath}";

            _databaseHelper = new DatabaseHelper(connectionString);
            _repository = new TemplateRepository(_databaseHelper);
            _parser = new TemplateParser(_repository);
        }

        public void Dispose()
        {
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
                    // Ignore failures during cleanup
                }
            }
        }

        [Fact]
        public async Task ProcessPromptAsync_ShouldReplaceSimpleTemplates()
        {
            // Arrange
            await _repository.InsertAsync(new TemplateModel { Key = "style", Value = "photorealistic oil painting" });
            await _repository.InsertAsync(new TemplateModel { Key = "subject", Value = "mystic owl" });

            string prompt = "A beautiful {style} of a {subject}.";

            // Act
            string result = await _parser.ProcessPromptAsync(prompt);

            // Assert
            result.Should().Be("A beautiful photorealistic oil painting of a mystic owl.");
        }

        [Fact]
        public async Task ProcessPromptAsync_ShouldReplaceTemplatesWithParameters()
        {
            // Arrange
            await _repository.InsertAsync(new TemplateModel 
            { 
                Key = "render", 
                Value = "a professional 3D render of {0} with a {1} background" 
            });

            string prompt = "Generate {render:a cute red panda:neon blue}";

            // Act
            string result = await _parser.ProcessPromptAsync(prompt);

            // Assert
            result.Should().Be("Generate a professional 3D render of a cute red panda with a neon blue background");
        }

        [Fact]
        public async Task ProcessPromptAsync_ShouldResolveRecursiveTemplates()
        {
            // Arrange
            await _repository.InsertAsync(new TemplateModel { Key = "inner", Value = "glowing magic" });
            await _repository.InsertAsync(new TemplateModel { Key = "outer", Value = "a bottle filled with {inner}" });

            string prompt = "Illustration of {outer}";

            // Act
            string result = await _parser.ProcessPromptAsync(prompt);

            // Assert
            result.Should().Be("Illustration of a bottle filled with glowing magic");
        }

        [Fact]
        public async Task ProcessPromptAsync_ShouldThrowKeyNotFoundException_OnUnresolvableTemplates()
        {
            // Arrange
            await _repository.InsertAsync(new TemplateModel { Key = "existing", Value = "resolved value" });

            string prompt = "A {existing} and a {non_existent_key} should fail.";

            // Act
            Func<Task> act = async () => await _parser.ProcessPromptAsync(prompt);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*'non_existent_key' n'est pas reconnu*");
        }

        [Fact]
        public async Task ProcessPromptAsync_ShouldThrowInvalidOperationException_OnInfiniteRecursion()
        {
            // Arrange
            // Set up a loop: {loop} resolves to "nested {loop}"
            await _repository.InsertAsync(new TemplateModel { Key = "loop", Value = "nested {loop}" });

            string prompt = "Start {loop} End";

            // Act
            Func<Task> act = async () => await _parser.ProcessPromptAsync(prompt);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*récursion infinie*");
        }

        [Theory]
        [InlineData("Some text {unclosed", "Accolade ouvrante '{' non fermée.")]
        [InlineData("Some text } premature", "Accolade fermante '}' inattendue ou non ouverte.")]
        [InlineData("Nested braces {outer {inner}} not supported", "Accolades imbriquées non supportées dans le prompt.")]
        public async Task ProcessPromptAsync_ShouldThrowFormatException_OnBraceSyntaxErrors(string invalidPrompt, string expectedError)
        {
            // Act
            Func<Task> act = async () => await _parser.ProcessPromptAsync(invalidPrompt);

            // Assert
            await act.Should().ThrowAsync<FormatException>()
                .WithMessage(expectedError);
        }

        [Fact]
        public async Task ProcessPromptAsync_ShouldCleanUpMultipleSpacesAndTrim()
        {
            // Arrange
            await _repository.InsertAsync(new TemplateModel { Key = "extra", Value = "   my   value   " });
            
            string prompt = "   Hello   world!    Here is {extra}   with spaces.   ";

            // Act
            string result = await _parser.ProcessPromptAsync(prompt);

            // Assert
            result.Should().Be("Hello world! Here is my value with spaces.");
        }

        [Fact]
        public async Task ProcessPromptAsync_ShouldReturnEmptyString_WhenPromptIsNullOrEmpty()
        {
            // Act & Assert
            (await _parser.ProcessPromptAsync(null!)).Should().BeEmpty();
            (await _parser.ProcessPromptAsync("   ")).Should().BeEmpty();
            (await _parser.ProcessPromptAsync("")).Should().BeEmpty();
        }
    }
}
