// See https://aka.ms/new-console-template for more information

using ProjectGenerator;

Console.WriteLine("Hello, World!");

var unityProject = UnityProject.LoadFromPath(args[0]);
foreach (var pkg in unityProject.PackageEntries)
{
    Console.WriteLine(pkg);
}
using var solutionContext = unityProject.Load();
