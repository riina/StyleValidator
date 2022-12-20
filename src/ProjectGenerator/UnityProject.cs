using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ProjectGenerator;

public partial class UnityProject : ProjectCollection
{
    public IReadOnlyCollection<UnityProjectPackageEntry> PackageEntries => _unityProjectPackageEntries;

    private readonly UnityProjectPackageEntry[] _unityProjectPackageEntries;

    private UnityProject(UnityProjectPackageEntry[] unityProjectPackageEntries)
    {
        _unityProjectPackageEntries = unityProjectPackageEntries;
    }

    public static UnityProject LoadFromPath(string path)
    {
        UnityProjectManifest manifest;
        using (var stream = File.OpenRead(Path.GetFullPath(Path.Combine(path, "Packages", "manifest.json"))))
        {
            manifest = JsonSerializer.Deserialize<UnityProjectManifest>(stream) ?? throw new InvalidDataException("Missing manifest content");
        }
        var entries = new List<UnityProjectPackageEntry>();
        string packagesPath = Path.GetFullPath(Path.Combine(path, "Packages"));
        foreach ((string name, string version) in manifest.Dependencies)
        {
            if (FileEntryRegex().Match(version) is { Success: true } fileEntryMatch)
            {
                string localPath = fileEntryMatch.Groups["path"].Value;
                string combinedPath = Path.GetFullPath(localPath, packagesPath);
                entries.Add(new LocalUnityProjectPackageEntry(name, combinedPath));
            }
        }
        return new UnityProject(entries.ToArray());
    }

    private record UnityProjectManifest([property: JsonPropertyName("dependencies")] IReadOnlyDictionary<string, string> Dependencies);

    [GeneratedRegex("file:(?<path>.+)")]
    private static partial Regex FileEntryRegex();

    public override SolutionContext Load()
    {
        var created = new List<ProjectFile>();
        List<string> asmDefPaths = new(), asmRefPaths = new();
        Dictionary<string, AsmDefEntry> asmDefs = new();
        Dictionary<string, List<AsmDefEntry>> asmDefReferences = new();
        Dictionary<string, List<string>> unknownAsmDefReferences = new();
        Dictionary<string, List<string>> asmDefReferencesInv = new();
        Dictionary<string, List<AsmRefEntry>> asmRefs = new();
        HashSet<string> keyPaths = new();
        try
        {
            foreach (var entry in _unityProjectPackageEntries)
            {
                entry.ExtractProjectFiles(out var subAsmDefs, out var subAsmRefs);
                asmDefPaths.AddRange(subAsmDefs);
                asmRefPaths.AddRange(subAsmRefs);
            }
            foreach (string asmDefPath in asmDefPaths)
            {
                keyPaths.Add(Path.GetDirectoryName(asmDefPath) ?? throw new InvalidDataException());
                string guid = ExtractMetaGuid(asmDefPath);
                AsmDef asmDef;
                using (var stream = File.OpenRead(asmDefPath))
                {
                    asmDef = JsonSerializer.Deserialize<AsmDef>(stream) ?? throw new InvalidDataException();
                }
                var entry = new AsmDefEntry(asmDefPath, guid, asmDef);
                asmDefs.Add(guid, entry);
                asmDefReferences.Add(guid, new List<AsmDefEntry>());
                unknownAsmDefReferences.Add(guid, new List<string>());
                foreach (string reference in asmDef.References)
                {
                    if (AsmGuidRegex().Match(reference) is not { Success: true } referenceGuidMatch)
                    {
                        throw new InvalidDataException();
                    }
                    string referenceGuidStr = referenceGuidMatch.Groups["guid"].Value;
                    if (!asmDefReferencesInv.TryGetValue(referenceGuidStr, out var collection))
                    {
                        collection = asmDefReferencesInv[referenceGuidStr] = new List<string>();
                    }
                    collection.Add(guid);
                }
                asmRefs.Add(guid, new List<AsmRefEntry>());
            }
            foreach (string asmRefPath in asmRefPaths)
            {
                keyPaths.Add(Path.GetDirectoryName(asmRefPath) ?? throw new InvalidDataException());
                string guid = ExtractMetaGuid(asmRefPath);
                AsmRef asmRef;
                using (var stream = File.OpenRead(asmRefPath))
                {
                    asmRef = JsonSerializer.Deserialize<AsmRef>(stream) ?? throw new InvalidDataException();
                }
                if (AsmGuidRegex().Match(asmRef.Reference) is not { Success: true } asmRefGuid)
                {
                    throw new InvalidDataException();
                }
                string referenceGuid = asmRefGuid.Groups["guid"].Value.ToLowerInvariant();
                AsmRefEntry asmRefEntry = new(asmRefPath, guid, referenceGuid);
                if (!asmRefs.TryGetValue(referenceGuid, out var collection))
                {
                    collection = asmRefs[referenceGuid] = new List<AsmRefEntry>();
                }
                collection.Add(asmRefEntry);
            }
            foreach (var asmRef in asmRefs)
            {
                if (!asmDefs.ContainsKey(asmRef.Key))
                {
                    throw new IOException($"Unknown source assembly for {asmRef.Key}");
                }
            }
            // TODO check unknown asmdef references
            foreach (var pair in asmDefReferencesInv)
            {
                if (asmDefs.TryGetValue(pair.Key, out var knownAsmDefEntry))
                {
                    foreach (string value in pair.Value)
                    {
                        asmDefReferences[value].Add(knownAsmDefEntry);
                    }
                }
                else
                {
                    foreach (string value in pair.Value)
                    {
                        unknownAsmDefReferences[value].Add(value);
                    }
                }
            }
            foreach (var asmDef in asmDefs.Values)
            {
                // TODO
                Console.WriteLine(asmDef.Path);
                foreach (var asmDefReference in asmDefReferences[asmDef.Guid])
                {
                    Console.WriteLine(" >!" + asmDefReference.Path);
                }
                foreach (string unknownAsmDefReference in unknownAsmDefReferences[asmDef.Guid])
                {
                    Console.WriteLine(" >?" + unknownAsmDefReference);
                }
                foreach (var asmRef in asmRefs[asmDef.Guid])
                {
                    Console.WriteLine(" -" + asmRef.Path);
                }
            }
        }
        catch (Exception e)
        {
            List<Exception> exc = new();
            foreach (var v in created)
            {
                try
                {
                    v.Dispose();
                }
                catch (Exception eSub)
                {
                    exc.Add(eSub);
                }
            }
            if (exc.Count != 0)
            {
                throw new AggregateException(e, new AggregateException(exc));
            }
            throw;
        }
        return new UnitySolutionContext(created.ToArray());
    }

    private static string ExtractMetaGuid(string assetPath)
    {
        string metaPath = $"{assetPath}.meta";
        using var reader = File.OpenText(metaPath);
        while (reader.ReadLine() is { } line)
        {
            if (GuidRegex().Match(line) is { Success: true } guidMatch)
            {
                return guidMatch.Groups["guid"].Value.ToLowerInvariant();
            }
        }
        throw new InvalidDataException("Missing guid in meta file");
    }

    private record AsmDefEntry(string Path, string Guid, AsmDef Value);

    private record AsmRefEntry(string Path, string Guid, string ReferenceGuid);

    private record AsmDef([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("references")] IReadOnlyList<string> References, [property: JsonPropertyName("versionDefines")] IReadOnlyList<AsmDef.VersionDefine> VersionDefines)
    {
        public record VersionDefine([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("expression")] string Expression, [property: JsonPropertyName("define")] string Define);
    }

    private record AsmRef([property: JsonPropertyName("reference")] string Reference);

    [GeneratedRegex("guid: (?<guid>[A-Fa-f0-9]{16})")]
    private static partial Regex GuidRegex();

    [GeneratedRegex("GUID:(?<guid>[A-Fa-f0-9]{16})")]
    private static partial Regex AsmGuidRegex();
}
