// AI Image generator. A program to generate image from AI API.
// Copyright (C) 2026  Martin Labelle
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ImageGeneratorApp
{
    public static class UserIdHelper
    {
        private static string? _cachedDefaultUserId;

        public static async Task<string> GetOpaqueUserIdAsync()
        {
            if (_cachedDefaultUserId != null)
            {
                return _cachedDefaultUserId;
            }

            try
            {
                // 🛡️ Sentinel: Prevent PII leakage by using a stable GUID instead of Environment.UserName
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImageGeneratorApp");
                string filePath = Path.Combine(folder, "device_id.txt");

                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (fs.Length <= 1024)
                        {
                            using (var reader = new StreamReader(fs))
                            {
                                _cachedDefaultUserId = (await reader.ReadToEndAsync()).Trim();
                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // File doesn't exist, will create a new one below
                }
                catch (DirectoryNotFoundException)
                {
                    // Directory doesn't exist, will create a new one below
                }

                if (string.IsNullOrEmpty(_cachedDefaultUserId))
                {
                    await Task.Run(() => Directory.CreateDirectory(folder));
                    _cachedDefaultUserId = Guid.NewGuid().ToString("N");
                    await File.WriteAllTextAsync(filePath, _cachedDefaultUserId);
                }
            }
            catch
            {
                // Fallback for session if IO fails
                _cachedDefaultUserId = Guid.NewGuid().ToString("N");
            }

            return _cachedDefaultUserId;
        }
    }
}