namespace ProjectGenerator;

public abstract class SolutionContext : IDisposable
{
    public abstract string Path { get; }

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
