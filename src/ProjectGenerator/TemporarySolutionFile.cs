namespace ProjectGenerator;

public class TemporarySolutionFile : SolutionFile
{
    public FileStream FileStream
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TemporarySolutionFile));
            }
            return _fileStream;
        }
    }

    private readonly FileStream _fileStream;
    private bool _disposed;

    public TemporarySolutionFile(string filePath) : base(filePath)
    {
        _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
    }

    public override Stream GetStream() => new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        if (disposing)
        {
            _fileStream.Dispose();
        }
    }

    public static TemporarySolutionFile CreateTemporaryProjectFile()
    {
        string directory = Path.GetTempPath();
        string filePath = Path.Combine(directory, $"{Guid.NewGuid()}.sln");
        return new TemporarySolutionFile(filePath);
    }
}
