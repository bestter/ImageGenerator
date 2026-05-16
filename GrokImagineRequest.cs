using System.Text.Json.Serialization;

namespace GrokImagineApp
{
    public class GrokImagineRequest
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