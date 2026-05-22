using System.Text.Json.Serialization;

namespace ImageGeneratorApp
{
    public class ImageGeneratorResponse
    {
        [JsonPropertyName("data")]
        public ImageGeneratorResponseData[]? Data { get; set; }
    }

    public class ImageGeneratorResponseData
    {
        [JsonPropertyName("b64_json")]
        public string? B64Json { get; set; }
    }
}
