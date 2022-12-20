using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProjectGenerator.UnitySupport;

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

    [GeneratedRegex("file:(?<path>.+)")]
    private static partial Regex FileEntryRegex();

    public override SolutionContext Load()
    {
        var created = new List<ProjectFile>();
        List<string> asmDefPaths = new(), asmRefPaths = new();
        Dictionary<string, TempProjectData> tempProjects = new();
        Dictionary<string, AsmDefEntry> asmDefs = new();
        Dictionary<string, List<AsmDefEntry>> asmDefReferences = new();
        Dictionary<string, List<string>> unknownAsmDefReferences = new();
        Dictionary<string, List<string>> asmDefReferencesInv = new();
        Dictionary<string, List<AsmRefEntry>> asmRefs = new();
        Dictionary<string, SemanticVersioning.Version> versions = new();
        HashSet<string> keyPaths = new();
        try
        {
            foreach (var entry in _unityProjectPackageEntries)
            {
                if (entry is LocalUnityProjectPackageEntry localUnityProjectPackageEntry)
                {
                    Package package;
                    using (var stream = File.OpenRead(Path.Combine(localUnityProjectPackageEntry.FullPath, "package.json")))
                    {
                        package = JsonSerializer.Deserialize<Package>(stream) ?? throw new InvalidDataException();
                    }
                    versions.Add(package.Name, SemanticVersioning.Version.Parse(package.Version));
                }
                entry.ExtractProjectFiles(out var subAsmDefs, out var subAsmRefs);
                asmDefPaths.AddRange(subAsmDefs);
                asmRefPaths.AddRange(subAsmRefs);
            }
            foreach (string asmDefPath in asmDefPaths)
            {
                string directory = Path.GetDirectoryName(asmDefPath) ?? throw new InvalidDataException();
                keyPaths.Add(directory);
                string guid = ExtractMetaGuid(asmDefPath);
                AsmDef asmDef;
                using (var stream = File.OpenRead(asmDefPath))
                {
                    asmDef = JsonSerializer.Deserialize<AsmDef>(stream) ?? throw new InvalidDataException();
                }
                var entry = new AsmDefEntry(directory, guid, asmDef);
                asmDefs.Add(guid, entry);
                asmDefReferences.Add(guid, new List<AsmDefEntry>());
                unknownAsmDefReferences.Add(guid, new List<string>());
                foreach (string reference in asmDef.References)
                {
                    string referenceGuid = ParseAsmGuid(reference);
                    if (!asmDefReferencesInv.TryGetValue(referenceGuid, out var collection))
                    {
                        collection = asmDefReferencesInv[referenceGuid] = new List<string>();
                    }
                    collection.Add(guid);
                }
                asmRefs.Add(guid, new List<AsmRefEntry>());
                tempProjects.Add(guid, new TempProjectData(GenerateTempPath(guid, ".csproj")));
            }
            foreach (string asmRefPath in asmRefPaths)
            {
                string directory = Path.GetDirectoryName(asmRefPath) ?? throw new InvalidDataException();
                keyPaths.Add(directory);
                string guid = ExtractMetaGuid(asmRefPath);
                AsmRef asmRef;
                using (var stream = File.OpenRead(asmRefPath))
                {
                    asmRef = JsonSerializer.Deserialize<AsmRef>(stream) ?? throw new InvalidDataException();
                }
                string referenceGuid = ParseAsmGuid(asmRef.Reference);
                AsmRefEntry asmRefEntry = new(directory, guid, referenceGuid);
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
                        unknownAsmDefReferences[value].Add(pair.Key);
                    }
                }
            }
            foreach (var asmDef in asmDefs.Values)
            {
                var tempProject = tempProjects[asmDef.Guid];
                // currently not supporting referenced assemblies
                // currently not supporting referenced but unresolved asmdefs
                tempProject.ReferencedCsprojPaths.AddRange(asmDefReferences[asmDef.Guid].Select(v => tempProjects[v.Guid].DestinationCsprojPath));
                AddSourceFiles(asmDef.Path, keyPaths, tempProject.SourceFiles, "*.cs");
                foreach (var asmRef in asmRefs[asmDef.Guid])
                {
                    AddSourceFiles(asmRef.Path, keyPaths, tempProject.SourceFiles, "*.cs");
                }
                foreach (var versionDefine in asmDef.Value.VersionDefines)
                {
                    // currently not supporting referenced but unresolved packages
                    if (versions.TryGetValue(versionDefine.Name, out var version))
                    {
                        if (IsVersionDefineSatisfied(versionDefine.Expression, version))
                        {
                            tempProject.Defines.Add(versionDefine.Define);
                        }
                        else
                        {
                            tempProject.FailedDefines.Add(versionDefine.Define);
                        }
                    }
                    else
                    {
                        tempProject.FailedDefines.Add(versionDefine.Define);
                    }
                }
                Console.WriteLine(asmDef.Path);
                foreach (string define in tempProject.Defines)
                {
                    Console.WriteLine(" +" + define);
                }
                foreach (string define in tempProject.FailedDefines)
                {
                    Console.WriteLine(" -" + define);
                }
                foreach (string file in tempProject.SourceFiles)
                {
                    Console.WriteLine(" >" + file);
                }
                // TODO process defines
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

    private static bool IsVersionDefineSatisfied(string versionDefineExpression, SemanticVersioning.Version existingVersion)
    {
        // https://docs.unity3d.com/2022.2/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html#version-define-expressions
        if (VersionDefineRangeRegex().Match(versionDefineExpression) is { Success: true } versionDefineRangeRegex)
        {
            bool minExc = versionDefineRangeRegex.Groups["leftBoundType"].Value == "(";
            bool maxExc = versionDefineRangeRegex.Groups["rightBoundType"].Value == ")";
            var left = SemanticVersioning.Version.Parse(versionDefineRangeRegex.Groups["left"].Value);
            var right = SemanticVersioning.Version.Parse(versionDefineRangeRegex.Groups["right"].Value);
            if (minExc)
            {
                if (existingVersion <= left)
                {
                    return false;
                }
            }
            else
            {
                if (existingVersion < left)
                {
                    return false;
                }
            }
            if (maxExc)
            {
                if (existingVersion >= right)
                {
                    return false;
                }
            }
            else
            {
                if (existingVersion > right)
                {
                    return false;
                }
            }
            return false;
            // TODO
        }
        if (VersionDefineExactRegex().Match(versionDefineExpression) is { Success: true } versionDefineExactRegex)
        {
            var define = SemanticVersioning.Version.Parse(versionDefineExactRegex.Groups["value"].Value);
            return existingVersion == define;
        }
        if (VersionDefineShortcutRegex().Match(versionDefineExpression) is { Success: true } versionDefineShortcutRegex)
        {
            var define = SemanticVersioning.Version.Parse(versionDefineShortcutRegex.Groups["value"].Value);
            return existingVersion >= define;
        }
        throw new InvalidDataException();
    }

    [GeneratedRegex(@"(?<leftBoundType>[\[\(])\s*(?<left>\S+?)\s*,\s*(?<right>\S+)\s*(?<rightBoundType>[\]\)])")]
    private static partial Regex VersionDefineRangeRegex();

    [GeneratedRegex(@"\[\s*(?<value>\S+)\s*\]")]
    private static partial Regex VersionDefineExactRegex();

    [GeneratedRegex(@"(?<value>\S+)")]
    private static partial Regex VersionDefineShortcutRegex();

    private static void AddSourceFiles(string directory, IReadOnlySet<string> keyPaths, ICollection<string> paths, string searchPattern)
    {
        foreach (string file in Directory.GetFiles(directory, searchPattern))
        {
            paths.Add(file);
        }
        foreach (string subDirectory in Directory.GetDirectories(directory))
        {
            AddSubSourceFiles(subDirectory, keyPaths, paths, searchPattern);
        }
    }

    private static void AddSubSourceFiles(string directory, IReadOnlySet<string> keyPaths, ICollection<string> paths, string searchPattern)
    {
        if (keyPaths.Contains(directory))
        {
            return;
        }
        AddSourceFiles(directory, keyPaths, paths, searchPattern);
    }

    private static string GenerateTempPath(string baseGuid, string ext)
    {
        return Path.Combine(Path.GetTempPath(), $"{baseGuid}_{Guid.NewGuid()}{ext}");
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

    private static string ParseAsmGuid(string referenceString)
    {
        if (AsmGuidRegex().Match(referenceString) is not { Success: true } referenceGuidMatch)
        {
            throw new InvalidDataException();
        }
        return referenceGuidMatch.Groups["guid"].Value.ToLowerInvariant();
    }

    [GeneratedRegex("guid: (?<guid>[A-Fa-f0-9]{16})")]
    private static partial Regex GuidRegex();

    [GeneratedRegex("GUID:(?<guid>[A-Fa-f0-9]{16})")]
    private static partial Regex AsmGuidRegex();
}
