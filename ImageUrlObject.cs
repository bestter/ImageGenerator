using System.Text.Json.Serialization;

namespace ImageGeneratorApp
{
    public class ImageUrlObject
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "image_url";

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
