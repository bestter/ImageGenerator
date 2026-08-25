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
            // Sanitize provider name to avoid path traversal (though it's hardcoded internally)
            string baseFileName = Path.GetFileName(provider);
            string safeProvider = string.Concat(baseFileName.Split(Path.GetInvalidFileNameChars()));

            string targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImageGeneratorApp"
            );

            // Normalize directory and ensure trailing separator for secure prefix check
            string normalizedTargetDir = Path.GetFullPath(targetDir);
            if (!normalizedTargetDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                normalizedTargetDir += Path.DirectorySeparatorChar;
            }

            string combinedPath = Path.Combine(normalizedTargetDir, $"ApiKey_{safeProvider}.dat");
            string normalizedCombinedPath = Path.GetFullPath(combinedPath);

            // SÉCURITÉ : Validation stricte StartsWith pour prévenir les fuites de répertoires
            if (!normalizedCombinedPath.StartsWith(normalizedTargetDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Détection de tentative de traversée de chemin dans le nom du provider.");
            }

            return normalizedCombinedPath;
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