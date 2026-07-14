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
        private readonly string _isolatedFolderPath;

        public UserIdHelperTests()
        {
            _testFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImageGeneratorApp");
            _testFilePath = Path.Combine(_testFolderPath, "device_id.txt");
            // Isolated temp folder so DirectoryNotFound tests never delete the shared LocalApplicationData directory
            // used in parallel by other test classes (ApiKeyStorageHelper, DatabaseHelper, etc.).
            _isolatedFolderPath = Path.Combine(Path.GetTempPath(), "ImageGeneratorApp_UserIdTest_" + Guid.NewGuid().ToString("N"));

            // Ensure a clean state before tests
            ResetCache();
            UserIdHelper.AppFolderOverride = null;
        }

        public void Dispose()
        {
            ResetCache();
            UserIdHelper.AppFolderOverride = null;

            // Try to clean up the test file if we can
            try
            {
                if (File.Exists(_testFilePath))
                {
                    File.Delete(_testFilePath);
                }
            }
            catch { /* Ignore cleanup errors */ }

            try
            {
                if (Directory.Exists(_isolatedFolderPath))
                {
                    Directory.Delete(_isolatedFolderPath, true);
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
            // Arrange — use an isolated temp folder that does not exist yet (no shared LocalAppData collision)
            string isolatedFilePath = Path.Combine(_isolatedFolderPath, "device_id.txt");
            if (Directory.Exists(_isolatedFolderPath))
            {
                Directory.Delete(_isolatedFolderPath, true);
            }

            UserIdHelper.AppFolderOverride = _isolatedFolderPath;
            ResetCache();

            // Act
            string result = await UserIdHelper.GetOpaqueUserIdAsync();

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            result.Length.Should().Be(32); // GUID "N" format length
            Guid.TryParseExact(result, "N", out _).Should().BeTrue();

            // Verify that the directory and file were created, and the file contains the returned ID
            Directory.Exists(_isolatedFolderPath).Should().BeTrue();
            File.Exists(isolatedFilePath).Should().BeTrue();

            string fileContent = await File.ReadAllTextAsync(isolatedFilePath, TestContext.Current.CancellationToken);
            fileContent.Should().Be(result);
        }
    }
}