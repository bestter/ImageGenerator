using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ImageGeneratorApp
{
    public static class ApiKeyStorageHelper
    {
        private static string GetStorageFilePath(string provider)
        {
            // Sanitize provider name to avoid path traversal (though it's hardcoded internally)
            string safeProvider = string.Concat(provider.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImageGeneratorApp",
                $"ApiKey_{safeProvider}.dat"
            );
        }

        public static void SaveApiKey(string provider, string apiKey)
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
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(filePath, encryptedBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save API key for {provider}: {ex.Message}");
                // Silently fail on storage errors for the caller, but log for debugging
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

                    byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load API key for {provider}: {ex.Message}");
                // Return empty if fails to load/unprotect
            }
            return string.Empty;
        }
    }
}