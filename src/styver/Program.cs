// See https://aka.ms/new-console-template for more information

using ProjectGenerator.UnitySupport;

Console.WriteLine("Hello, World!");

var unityProject = UnityProject.LoadFromPath(args[0]);
using var solutionContext = unityProject.Load();
