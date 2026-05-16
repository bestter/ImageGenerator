using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace GrokImagineApp
{
    public static class UserIdHelper
    {
        public static string GetOpaqueUserId(string? identityName = null)
        {
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
                return builder.ToString();
            }
        }
    }
}