using FluentAssertions;
using ImageGeneratorApp;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    [Collection("Sequential")]
    public class UserIdHelperTests : IDisposable
    {
        private readonly string _testFolderPath;
        private readonly string _testFilePath;

        public UserIdHelperTests()
        {
            _testFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImageGeneratorApp");
            _testFilePath = Path.Combine(_testFolderPath, "device_id.txt");

            // Ensure a clean state before tests
            ResetCache();
        }

        public void Dispose()
        {
            ResetCache();
            // Try to clean up the test file if we can
            try
            {
                if (File.Exists(_testFilePath))
                {
                    File.Delete(_testFilePath);
                }
            }
            catch { /* Ignore cleanup errors */ }
        }

        private void ResetCache()
        {
            var field = typeof(UserIdHelper).GetField("_cachedDefaultUserId", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }

        [Fact]
        public async Task GetOpaqueUserIdAsync_ReturnsStableIdentifier()
        {
            // Act
            string hash1 = await UserIdHelper.GetOpaqueUserIdAsync();

            // Reset cache to force reading from file
            ResetCache();
            string hash2 = await UserIdHelper.GetOpaqueUserIdAsync();

            // Assert
            hash1.Should().NotBeNullOrWhiteSpace();
            hash1.Should().Be(hash2);
        }

        [Fact]
        public async Task GetOpaqueUserIdAsync_OnIoFailure_ReturnsFallbackGuid()
        {
            // Arrange
            Directory.CreateDirectory(_testFolderPath);

            // Create a file and lock it exclusively to simulate an IO failure when reading/writing
            using (var lockStream = new FileStream(_testFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                // Act
                string result = await UserIdHelper.GetOpaqueUserIdAsync();

                // Assert
                result.Should().NotBeNullOrWhiteSpace();
                result.Length.Should().Be(32); // GUID "N" format length

                // Verify it's a valid GUID
                Guid.TryParseExact(result, "N", out Guid parsed).Should().BeTrue();
            }
        }

        [Fact]
        public async Task GetOpaqueUserIdAsync_OnDirectoryNotFound_CreatesDirectoryAndReturnsNewId()
        {
            // Arrange
            // Ensure the directory does not exist to trigger DirectoryNotFoundException when opening the file stream
            if (Directory.Exists(_testFolderPath))
            {
                Directory.Delete(_testFolderPath, true);
            }

            // Act
            string result = await UserIdHelper.GetOpaqueUserIdAsync();

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            result.Length.Should().Be(32); // GUID "N" format length
            Guid.TryParseExact(result, "N", out _).Should().BeTrue();

            // Verify that the directory and file were created, and the file contains the returned ID
            Directory.Exists(_testFolderPath).Should().BeTrue();
            File.Exists(_testFilePath).Should().BeTrue();

            string fileContent = await File.ReadAllTextAsync(_testFilePath, TestContext.Current.CancellationToken);
            fileContent.Should().Be(result);
        }
    }
}