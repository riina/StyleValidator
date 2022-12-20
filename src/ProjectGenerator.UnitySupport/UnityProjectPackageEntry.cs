namespace ProjectGenerator.UnitySupport;

public abstract record UnityProjectPackageEntry(string Name)
{
    public abstract void ExtractProjectFiles(out List<string> asmDefs, out List<string> asmRefs);
}
