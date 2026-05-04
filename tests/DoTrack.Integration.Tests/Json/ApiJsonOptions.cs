using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoTrack.Integration.Tests.Json;

public static class ApiJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
