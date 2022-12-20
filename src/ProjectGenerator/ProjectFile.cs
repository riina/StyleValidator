namespace ProjectGenerator;

public abstract class ProjectFile : IDisposable
{
    public string FilePath { get; }

    public ProjectFile(string filePath) => FilePath = filePath;

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}