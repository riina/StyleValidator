using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectGenerator.UnitySupport;

internal record UnityProjectManifest([property: JsonPropertyName("dependencies")] IReadOnlyDictionary<string, string> Dependencies);
