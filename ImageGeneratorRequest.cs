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

using System.Text.Json.Serialization;


namespace ImageGeneratorApp
{
    public class ImageGeneratorRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public int N { get; set; } = 1;

        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = string.Empty;

        [JsonPropertyName("aspect_ratio")]
        public string AspectRatio { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("response_format")]
        public string ResponseFormat { get; set; } = "b64_json";

        [JsonPropertyName("image")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Image { get; set; }

        [JsonPropertyName("images")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object[]? Images { get; set; }
    }
}