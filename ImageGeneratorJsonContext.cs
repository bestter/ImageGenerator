using System.Text.Json.Serialization;

namespace GrokImagineApp
{
    [JsonSerializable(typeof(GrokImagineRequest))]
    [JsonSerializable(typeof(GrokImagineResponse))]
    internal partial class GrokImagineJsonContext : JsonSerializerContext
    {
    }
}
