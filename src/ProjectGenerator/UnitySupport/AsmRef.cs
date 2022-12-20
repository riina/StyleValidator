using System.Text.Json.Serialization;

namespace ProjectGenerator.UnitySupport;

internal record AsmRef([property: JsonPropertyName("reference")] string Reference);
