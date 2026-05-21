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

            // ⚡ Bolt Optimization: Use SHA256.HashData and Convert.ToHexStringLower.
            // This avoids allocating a SHA256 instance, a StringBuilder, and 32 intermediate string
            // allocations per hash calculation, significantly reducing Large Object Heap and GC pressure.
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexStringLower(bytes);
        }
    }
}