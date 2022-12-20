namespace ProjectGenerator.UnitySupport;

public class UnitySolutionContext : SolutionContext
{
    public override string Path
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnitySolutionContext));
            }
            return _solutionFile.FilePath;
        }
    }

    public IEnumerable<ProjectFile> ProjectFiles
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnitySolutionContext));
            }
            return _projectFiles;
        }
    }

    private readonly SolutionFile _solutionFile;
    private readonly ProjectFile[] _projectFiles;
    private bool _disposed;

    public UnitySolutionContext(SolutionFile solutionFile, ProjectFile[] projectFiles)
    {
        _solutionFile = solutionFile;
        _projectFiles = projectFiles;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            List<Exception> exc = new();
            foreach (var project in _projectFiles)
            {
                try
                {
                    project.Dispose();
                }
                catch (Exception eSub)
                {
                    exc.Add(eSub);
                }
            }
            if (exc.Count != 0)
            {
                throw new AggregateException(exc);
            }
        }
    }
}
