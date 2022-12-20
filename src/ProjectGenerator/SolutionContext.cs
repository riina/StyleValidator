using System;

namespace ProjectGenerator;

public class SolutionContext : IDisposable
{
    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
