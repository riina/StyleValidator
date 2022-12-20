using System.Diagnostics;

namespace styver;

public static class StyleFormatter
{
    public static async Task<int> ExecuteAsync(string solutionFile, bool verifyNoChanges, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo("dotnet");
        psi.ArgumentList.Add("format");
        if (verifyNoChanges)
        {
            psi.ArgumentList.Add("--verify-no-changes");
        }
        psi.ArgumentList.Add(solutionFile);
        var process = Process.Start(psi) ?? throw new IOException();
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
