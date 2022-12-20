namespace ProjectGenerator.UnitySupport;

internal class TempProjectData
{
    public string DestinationCsprojPath { get; }

    public List<string> ReferencedCsprojPaths { get; }

    public List<string> SourceFiles { get; }

    public List<string> Defines { get; }

    public List<string> FailedDefines { get; }

    public TempProjectData(string destinationCsprojPath)
    {
        DestinationCsprojPath = destinationCsprojPath;
        ReferencedCsprojPaths = new List<string>();
        SourceFiles = new List<string>();
        Defines = new List<string>();
        FailedDefines = new List<string>();
    }
}
