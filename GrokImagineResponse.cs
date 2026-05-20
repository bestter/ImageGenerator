using System.Text.Json.Serialization;

namespace GrokImagineApp
{
    public class GrokImagineResponse
    {
        [JsonPropertyName("data")]
        public GrokImagineResponseData[]? Data { get; set; }
    }

    public class GrokImagineResponseData
    {
        [JsonPropertyName("b64_json")]
        public string? B64Json { get; set; }
    }
}
