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

namespace ImageGeneratorApp.Tests
{
    public class HistoryItemCopiedEventArgsTests
    {
        [Fact]
        public void Constructor_AssignsPropertiesCorrectly()
        {
            // Arrange
            var expectedPrompt = "A beautiful sunset over the mountains";
            var expectedModelName = "gpt-4-vision-preview";

            // Act
            var eventArgs = new HistoryItemCopiedEventArgs(expectedPrompt, expectedModelName);

            // Assert
            eventArgs.Prompt.Should().Be(expectedPrompt);
            eventArgs.ModelName.Should().Be(expectedModelName);
        }

        [Fact]
        public void Constructor_HandlesNullPrompt()
        {
            // Arrange
            string? expectedPrompt = null;
            var expectedModelName = "dall-e-3";

            // Act
            var eventArgs = new HistoryItemCopiedEventArgs(expectedPrompt!, expectedModelName);

            // Assert
            eventArgs.Prompt.Should().BeNull();
            eventArgs.ModelName.Should().Be(expectedModelName);
        }

        [Fact]
        public void Constructor_HandlesNullModelName()
        {
            // Arrange
            var expectedPrompt = "A cute cat";
            string? expectedModelName = null;

            // Act
            var eventArgs = new HistoryItemCopiedEventArgs(expectedPrompt, expectedModelName!);

            // Assert
            eventArgs.Prompt.Should().Be(expectedPrompt);
            eventArgs.ModelName.Should().BeNull();
        }
    }
}
