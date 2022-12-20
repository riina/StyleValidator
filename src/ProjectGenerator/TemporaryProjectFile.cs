using System;
using System.IO;

namespace ProjectGenerator;

public class TemporaryProjectFile : ProjectFile
{
    private readonly FileStream _fileStream;
    private bool _disposed;

    public TemporaryProjectFile(string filePath) : base(filePath)
    {
        _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
    }

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

    public static TemporaryProjectFile CreateTemporaryProjectFile()
    {
        string directory = Path.GetTempPath();
        string filePath = Path.Combine(directory, $"{Guid.NewGuid()}.csproj");
        return new TemporaryProjectFile(filePath);
    }
}