using System.Collections.Generic;

namespace ProjectGenerator;

public abstract record UnityProjectPackageEntry(string Name)
{
    public abstract void ExtractProjectFiles(out List<string> asmDefs, out List<string> asmRefs);
}
