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
            if (identityName == null && _cachedDefaultUserId != null)
            {
                return _cachedDefaultUserId;
            }

            string name = identityName ?? "unknown_user";
            if (identityName == null)
            {
                try
                {
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        name = WindowsIdentity.GetCurrent().Name ?? "unknown_user";
                    }
                }
                catch
                {
                    // Fallback if anything goes wrong
                }
            }

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

                string result = builder.ToString();

                // ⚡ Bolt Optimization: Cache the computed hashed user ID for the default user.
                // WindowsIdentity.GetCurrent().Name is an expensive interop call.
                // Since the user doesn't change during the application's lifetime, caching prevents redundant P/Invoke and SHA256 computations per request.
                if (identityName == null)
                {
                    _cachedDefaultUserId = result;
                }

                return result;
            }
        }
    }
}