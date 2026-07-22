using System.Text.Json.Serialization;

namespace ImageGeneratorApp
{
    public class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public GeminiContent[]? Contents { get; set; }

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    public class GeminiContent
    {
        [JsonPropertyName("parts")]
        public GeminiPart[]? Parts { get; set; }
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("inlineData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiInlineData? InlineData { get; set; }
    }

    public class GeminiInlineData
    {
        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }

    public class GeminiGenerationConfig
    {
        [JsonPropertyName("responseModalities")]
        public string[]? ResponseModalities { get; set; }

        [JsonPropertyName("imageConfig")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiImageConfig? ImageConfig { get; set; }
    }

    public class GeminiImageConfig
    {
        [JsonPropertyName("aspectRatio")]
        public string? AspectRatio { get; set; }

        [JsonPropertyName("imageSize")]
        public string? ImageSize { get; set; }
    }

    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}