using System;
using System.Security.Cryptography;
using System.Text;

namespace GrokImagineApp
{
    public static class UserIdHelper
    {
        public static string? GetOpaqueUserId(string? identityName = null)
        {
            // 🛡️ Sentinel: Omit user tracking field entirely when no identity is provided to prevent PII leakage
            if (string.IsNullOrWhiteSpace(identityName))
            {
                return null;
            }

            string salt = "GrokImagineApp_Salt_2023";
            string rawData = identityName + salt;

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