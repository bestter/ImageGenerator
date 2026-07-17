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
        private static string GetStorageFilePath(string provider)
        {
            // Sanitize provider name to avoid path traversal (though it's hardcoded internally)
            string safeProvider = string.Concat(Path.GetFileName(provider).Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImageGeneratorApp",
                $"ApiKey_{safeProvider}.dat"
            );
        }

        public static async Task SaveApiKeyAsync(string provider, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return;

            try
            {
                string filePath = GetStorageFilePath(provider);
                var directory = Path.GetDirectoryName(filePath);
                byte[] plainBytes = Encoding.UTF8.GetBytes(apiKey);
                byte[] encryptedBytes;
                try
                {
                    encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plainBytes);
                }

                try
                {
                    await File.WriteAllBytesAsync(filePath, encryptedBytes);
                }
                catch (DirectoryNotFoundException)
                {
                    if (directory != null)
                    {
                        // ⚡ Bolt Optimization: Offload synchronous I/O from the async hot path to prevent thread pool starvation.
                        // Execute only in the rare fallback condition when the directory actually needs to be generated.
                        // 🛡️ Sentinel: Removed Directory.Exists check before CreateDirectory to prevent TOCTOU race conditions.
                        await Task.Run(() => Directory.CreateDirectory(directory));
                        await File.WriteAllBytesAsync(filePath, encryptedBytes);
                    }
                }
            }
            catch (IOException)
            {
                // Silently fail on storage errors
            }
            catch (UnauthorizedAccessException)
            {
                // Silently fail on permission errors
            }
            catch (CryptographicException)
            {
                // Silently fail on encryption errors
            }
        }

        public static async Task<string> LoadApiKeyAsync(string provider)
        {
            try
            {
                string filePath = GetStorageFilePath(provider);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
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
                        int read = await fs.ReadAsync(encryptedBytes, bytesRead, length - bytesRead);
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

                    byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
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
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to load API key for {provider}: {ex.Message}");
                // Return empty if fails to read file
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Failed to load API key for {provider}: {ex.Message}");
                // Return empty if permission denied
            }
            catch (CryptographicException ex)
            {
                Debug.WriteLine($"Failed to load API key for {provider}: {ex.Message}");
                // Return empty if unprotect fails
            }
            return string.Empty;
        }
    }
}