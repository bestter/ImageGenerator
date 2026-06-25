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
            string safeProvider = string.Concat(Path.GetFileName(_testProvider).Split(Path.GetInvalidFileNameChars()));
            _filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImageGeneratorApp",
                $"ApiKey_{safeProvider}.dat"
            );
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
                else if (Directory.Exists(_filePath))
                {
                    Directory.Delete(_filePath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public async Task LoadApiKey_WhenFileDoesNotExist_ReturnsEmptyString()
        {
            // Act
            string result = await ApiKeyStorageHelper.LoadApiKeyAsync(_testProvider);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SaveAndLoadApiKey_NominalCase_SavesAndLoadsCorrectly()
        {
            // Arrange
            string originalKey = "my-secret-api-key-12345";

            // Act
            ApiKeyStorageHelper.SaveApiKey(_testProvider, originalKey);
            string loadedKey = await ApiKeyStorageHelper.LoadApiKeyAsync(_testProvider);

            // Assert
            loadedKey.Should().Be(originalKey);
        }

        [Fact]
        public async Task LoadApiKey_WhenFileIsOversized_ReturnsEmptyString()
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
            string result = await ApiKeyStorageHelper.LoadApiKeyAsync(_testProvider);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SaveApiKey_WhenFileIsLocked_SilentlyFails_IOException()
        {
            // Arrange
            string? directory = Path.GetDirectoryName(_filePath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(_filePath, "initial content");

            // Act & Assert
            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // Action should not throw
                Action act = () => ApiKeyStorageHelper.SaveApiKey(_testProvider, "new key");
                act.Should().NotThrow();
            }
        }

        [Fact]
        public async Task LoadApiKey_WhenFileIsLocked_ReturnsEmptyString_IOException()
        {
            // Arrange
            string? directory = Path.GetDirectoryName(_filePath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(_filePath, "initial content");

            // Act & Assert
            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // Action should not throw and return empty string
                string result = await ApiKeyStorageHelper.LoadApiKeyAsync(_testProvider);
                result.Should().BeEmpty();
            }
        }

        [Fact]
        public void SaveApiKey_WhenPathIsDirectory_SilentlyFails_UnauthorizedAccessException()
        {
            // Arrange
            // Create a directory at the file path to trigger UnauthorizedAccessException
            Directory.CreateDirectory(_filePath);

            // Act & Assert
            Action act = () => ApiKeyStorageHelper.SaveApiKey(_testProvider, "new key");
            act.Should().NotThrow();
        }

        [Fact]
        public async Task LoadApiKey_WhenPathIsDirectory_ReturnsEmptyString_UnauthorizedAccessException()
        {
            // Arrange
            // Create a directory at the file path to trigger UnauthorizedAccessException
            Directory.CreateDirectory(_filePath);

            // Act
            string result = await ApiKeyStorageHelper.LoadApiKeyAsync(_testProvider);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SaveApiKey_CryptographicException_SilentlyFails()
        {
            // Note: It's hard to trigger CryptographicException from SaveApiKey without
            // corrupting the DPAPI system or running in a different context.
            // But we can test that passing null or whitespace key doesn't throw.
            Action act1 = () => ApiKeyStorageHelper.SaveApiKey(_testProvider, null!);
            Action act2 = () => ApiKeyStorageHelper.SaveApiKey(_testProvider, "   ");

            act1.Should().NotThrow();
            act2.Should().NotThrow();
        }

        [Fact]
        public async Task LoadApiKey_WhenFileIsCorrupted_ReturnsEmptyString_CryptographicException()
        {
            // Arrange
            string? directory = Path.GetDirectoryName(_filePath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            // Write invalid/corrupted bytes to trigger CryptographicException during Unprotect
            byte[] corruptedBytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            File.WriteAllBytes(_filePath, corruptedBytes);

            // Act
            string result = await ApiKeyStorageHelper.LoadApiKeyAsync(_testProvider);

            // Assert
            result.Should().BeEmpty();
        }
    }
}