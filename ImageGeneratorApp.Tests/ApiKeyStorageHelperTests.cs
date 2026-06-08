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
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class ApiKeyStorageHelperTests : IDisposable
    {
        private readonly string _testProvider;
        private readonly string _filePath;

        public ApiKeyStorageHelperTests()
        {
            _testProvider = "TestProvider_" + Guid.NewGuid().ToString("N");
            string safeProvider = string.Concat(_testProvider.Split(Path.GetInvalidFileNameChars()));
            _filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImageGeneratorApp",
                $"ApiKey_{safeProvider}.dat"
            );
        }

        public void Dispose()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    File.Delete(_filePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public void LoadApiKey_WhenFileDoesNotExist_ReturnsEmptyString()
        {
            // Act
            string result = ApiKeyStorageHelper.LoadApiKey(_testProvider);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SaveAndLoadApiKey_NominalCase_SavesAndLoadsCorrectly()
        {
            // Arrange
            string originalKey = "my-secret-api-key-12345";

            // Act
            ApiKeyStorageHelper.SaveApiKey(_testProvider, originalKey);
            string loadedKey = ApiKeyStorageHelper.LoadApiKey(_testProvider);

            // Assert
            loadedKey.Should().Be(originalKey);
        }

        [Fact]
        public void LoadApiKey_WhenFileIsOversized_ReturnsEmptyString()
        {
            // Arrange
            // Create a file larger than 4096 bytes (e.g. 4097 bytes)
            byte[] oversizedBytes = new byte[4097];
            new Random().NextBytes(oversizedBytes);

            string? directory = Path.GetDirectoryName(_filePath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(_filePath, oversizedBytes);

            // Act
            string result = ApiKeyStorageHelper.LoadApiKey(_testProvider);

            // Assert
            result.Should().BeEmpty();
        }
    }
}
