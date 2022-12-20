namespace ProjectGenerator;

public abstract class SolutionFile : IDisposable
{
    public string FilePath { get; }

    public SolutionFile(string filePath) => FilePath = filePath;

    public abstract Stream GetStream();

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
