// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using ProjectGenerator.UnitySupport;
using styver;

var unityProjectPathArg = new Argument<string>("unity-project-path",
    description: "Path to Unity project");
var noCheckOption = new Option<bool>(
    "--no-check",
    description: "Do not perform dotnet format");
var verifyNoChangesOption = new Option<bool>(
    "--verify-no-changes",
    description: "Only verify that no changes will be made");
var printProjectsOption = new Option<bool>(
    "--print-projects",
    description: "Print generated projects");
var rootCommand = new RootCommand { unityProjectPathArg, noCheckOption, verifyNoChangesOption, printProjectsOption };

rootCommand.Handler = CommandHandler.Create(async (string unityProjectPath, bool noCheck, bool verifyNoChanges, bool printProjects) =>
{
    var unityProject = UnityProject.LoadFromPath(unityProjectPath);
    using var solutionContext = unityProject.Load();
    if (printProjects)
    {
        foreach (var project in solutionContext.ProjectFiles)
        {
            Console.WriteLine($">> {project.FilePath}");
            await using var stream = project.GetStream();
            Console.WriteLine(await new StreamReader(stream).ReadToEndAsync());
        }
    }
    if (noCheck)
    {
        return;
    }
    await StyleFormatter.ExecuteAsync(solutionContext.Path, verifyNoChanges);
});
return await rootCommand.InvokeAsync(args);
