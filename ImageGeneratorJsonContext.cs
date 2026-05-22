using System.Text.Json.Serialization;

namespace ImageGeneratorApp
{
    [JsonSerializable(typeof(ImageGeneratorRequest))]
    [JsonSerializable(typeof(ImageGeneratorResponse))]
    internal partial class ImageGeneratorJsonContext : JsonSerializerContext
    {
    }
}
