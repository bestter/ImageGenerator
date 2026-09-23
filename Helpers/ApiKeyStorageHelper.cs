using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ImageGeneratorApp
{
    public static class ApiKeyStorageHelper
    {
        private static readonly byte[] s_entropy = Encoding.UTF8.GetBytes("ImageGeneratorApp_Entropy_v1");

        private static string GetStorageFilePath(string provider)
        {
            // 🛡️ Sentinel: Prevent path traversal by isolating the filename, normalizing the directory, and explicitly checking boundaries
            string baseFileName = Path.GetFileName(provider);
            string safeProvider = string.Concat(baseFileName.Split(Path.GetInvalidFileNameChars()));

            string targetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImageGeneratorApp");
            string normalizedTargetDirectory = Path.GetFullPath(targetDirectory);
            if (!normalizedTargetDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                normalizedTargetDirectory += Path.DirectorySeparatorChar;
            }

            string finalPath = Path.GetFullPath(Path.Combine(normalizedTargetDirectory, $"ApiKey_{safeProvider}.dat"));
            if (!finalPath.StartsWith(normalizedTargetDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid provider name resulting in path traversal.", nameof(provider));
            }

            return finalPath;
        }

        public static async Task SaveApiKeyAsync(string provider, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return;

            try
            {
                string filePath = GetStorageFilePath(provider);
                var directory = Path.GetDirectoryName(filePath);
                if (directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] plainBytes = Encoding.UTF8.GetBytes(apiKey);
                try
                {
                    byte[] encryptedBytes = ProtectedData.Protect(plainBytes, s_entropy, DataProtectionScope.CurrentUser);
                    await File.WriteAllBytesAsync(filePath, encryptedBytes).ConfigureAwait(false);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plainBytes);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine($"Failed to save API key for {provider}. A storage or permission error occurred.");
                // Fail on storage or permission errors
            }
            catch (CryptographicException)
            {
                Debug.WriteLine($"Failed to save API key for {provider}. A cryptographic error occurred.");
                // Fail on encryption errors
            }
        }

        public static string LoadApiKey(string provider)
        {
            try
            {
                string filePath = GetStorageFilePath(provider);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Length > 4096)
                    {
                        return string.Empty;
                    }

                    int length = (int)fs.Length;
                    byte[] encryptedBytes = new byte[length];
                    int bytesRead = 0;
                    while (bytesRead < length)
                    {
                        int read = fs.Read(encryptedBytes, bytesRead, length - bytesRead);
                        if (read == 0)
                        {
                            break;
                        }
                        bytesRead += read;
                    }

                    if (bytesRead != length)
                    {
                        return string.Empty;
                    }

                    byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, s_entropy, DataProtectionScope.CurrentUser);
                    try
                    {
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(plainBytes);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine($"Failed to load API key for {provider}. A storage or permission error occurred.");
                // Return empty if fails to read file or permission denied
            }
            catch (CryptographicException)
            {
                Debug.WriteLine($"Failed to load API key for {provider}. A cryptographic error occurred.");
                // Return empty if unprotect fails
            }
            return string.Empty;
        }
    }
}