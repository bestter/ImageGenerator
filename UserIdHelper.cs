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

namespace ImageGeneratorApp
{
    public static class UserIdHelper
    {
        private static string? _cachedDefaultUserId;

        public static string GetOpaqueUserId()
        {
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
    }
}
