using System.Text.Json.Serialization;

namespace ProjectGenerator.UnitySupport;

public record Package([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("version")] string Version);
