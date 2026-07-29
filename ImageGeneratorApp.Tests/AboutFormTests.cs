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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class AboutFormTests
    {
        [Fact]
        public void AboutForm_Constructor_InitializesPropertiesCorrectly()
        {
            // Act
            using var form = new AboutForm();

            // Assert
            form.Text.Should().Be("À propos de Générateur d'image");
            form.FormBorderStyle.Should().Be(FormBorderStyle.FixedDialog);
            form.MaximizeBox.Should().BeFalse();
            form.MinimizeBox.Should().BeFalse();
            form.StartPosition.Should().Be(FormStartPosition.CenterParent);
            form.Size.Should().Be(new Size(520, 400));
        }

        [Fact]
        public void AboutForm_Constructor_CreatesRequiredControls()
        {
            // Act
            using var form = new AboutForm();

            // Assert
            form.Controls.Count.Should().BeGreaterThan(0);

            // Find specific controls
            var labels = form.Controls.OfType<Label>().ToList();
            labels.Should().Contain(l => l.Text == "Générateur d'image", "Application name label should exist");
            labels.Should().Contain(l => l.Text.StartsWith("Version "), "Version label should exist");
            labels.Should().Contain(l => l.Text.Contains("©"), "Copyright label should exist");
            labels.Should().Contain(l => l.Text == "Avis de licence :", "License title label should exist");

            var textBoxes = form.Controls.OfType<TextBox>().ToList();
            textBoxes.Should().ContainSingle("There should be exactly one TextBox for the license");
            var licenseTextBox = textBoxes.First();
            licenseTextBox.ReadOnly.Should().BeTrue();
            licenseTextBox.Multiline.Should().BeTrue();
            licenseTextBox.Text.Should().Contain("Licence Publique Générale GNU");

            var buttons = form.Controls.OfType<Button>().ToList();
            buttons.Should().HaveCount(2, "There should be an OK button and a License button");
            buttons.Should().Contain(b => b.Text == "OK", "OK button should exist");
            buttons.Should().Contain(b => b.Text.Contains("licence complète"), "Show License button should exist");

            var okButton = buttons.First(b => b.Text == "OK");
            okButton.DialogResult.Should().Be(DialogResult.OK);
            form.AcceptButton.Should().BeSameAs(okButton);
        }
    }
}