using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace GrokImagineApp
{
    public static class UserIdHelper
    {
        private static string? _cachedDefaultUserId;

        public static string GetOpaqueUserId(string? identityName = null)
        {
            if (identityName != null)
            {
                return ComputeHash(identityName);
            }

            if (_cachedDefaultUserId != null)
            {
                return _cachedDefaultUserId;
            }

            try
            {
                // 🛡️ Sentinel: Prevent PII leakage by using a stable GUID instead of Environment.UserName
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GrokImagineApp");
                Directory.CreateDirectory(folder);
                string filePath = Path.Combine(folder, "device_id.txt");

                if (File.Exists(filePath))
                {
                    _cachedDefaultUserId = File.ReadAllText(filePath).Trim();
                }
                else
                {
                    _cachedDefaultUserId = Guid.NewGuid().ToString("N");
                    File.WriteAllText(filePath, _cachedDefaultUserId);
                }
            }
            catch
            {
                // Fallback for session if IO fails
                _cachedDefaultUserId = Guid.NewGuid().ToString("N");
            }

            return _cachedDefaultUserId;
        }

        private static string ComputeHash(string name)
        {
            string salt = "GrokImagineApp_Salt_2023";
            string rawData = name + salt;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}