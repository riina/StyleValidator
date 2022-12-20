using System;
using System.Collections.Generic;
using System.IO;

namespace ProjectGenerator;

public record LocalUnityProjectPackageEntry(string Name, string FullPath) : UnityProjectPackageEntry(Name)
{
    public override void ExtractProjectFiles(out List<string> asmDefs, out List<string> asmRefs)
    {
        asmDefs = new List<string>();
        asmRefs = new List<string>();
        foreach (string path in Directory.GetFiles(FullPath, "*.asm?ef", SearchOption.AllDirectories))
        {
            switch (Path.GetExtension(path.AsSpan()))
            {
                case ".asmdef":
                    asmDefs.Add(path);
                    break;
                case ".asmref":
                    asmRefs.Add(path);
                    break;
            }
        }
    }
}
