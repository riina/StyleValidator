using System.Collections.Generic;

namespace ProjectGenerator.UnitySupport;

internal class TempProjectData
{
    public string DestinationCsprojPath { get; }

    public List<string> ReferencedCsprojPaths { get; }

    public List<string> SourceFiles { get; }

    public TempProjectData(string destinationCsprojPath)
    {
        DestinationCsprojPath = destinationCsprojPath;
        ReferencedCsprojPaths = new List<string>();
        SourceFiles = new List<string>();
    }
}
