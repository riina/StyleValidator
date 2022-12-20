using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectGenerator.UnitySupport;

internal record AsmDef([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("references")] IReadOnlyList<string> References, [property: JsonPropertyName("versionDefines")] IReadOnlyList<AsmDef.VersionDefine> VersionDefines)
{
    public record VersionDefine([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("expression")] string Expression, [property: JsonPropertyName("define")] string Define);
}
