namespace ProjectGenerator.UnitySupport;

public class UnitySolutionContext : SolutionContext
{
    private readonly ProjectFile[] _projectFiles;

    public UnitySolutionContext(ProjectFile[] projectFiles)
    {
        _projectFiles = projectFiles;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
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
