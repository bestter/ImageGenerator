using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ImageGeneratorApp
{
    public static class ApiKeyStorageHelper
    {
        private static readonly string StorageFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageGeneratorApp",
            "ApiKey.dat"
        );

        public static void SaveApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return;

            try
            {
                var directory = Path.GetDirectoryName(StorageFilePath);
                if (directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] plainBytes = Encoding.UTF8.GetBytes(apiKey);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(StorageFilePath, encryptedBytes);
            }
            catch (Exception)
            {
                // Silently fail on storage errors
            }
        }

        public static string LoadApiKey()
        {
            try
            {
                if (File.Exists(StorageFilePath))
                {
                    byte[] encryptedBytes = File.ReadAllBytes(StorageFilePath);
                    byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
            catch (Exception)
            {
                // Return empty if fails to unprotect
            }
            return string.Empty;
        }
    }
}
