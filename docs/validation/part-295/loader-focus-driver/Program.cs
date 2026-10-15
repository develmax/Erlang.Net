using System.Reflection;
using System.Runtime.Loader;
var path = Path.GetFullPath(args[0]);
var resolver = new AssemblyDependencyResolver(path);
AssemblyLoadContext.Default.Resolving += (context, name) => resolver.ResolveAssemblyToPath(name) is { } dependency ? context.LoadFromAssemblyPath(dependency) : null;
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
foreach (string name in args.Skip(1))
{
    string[] parts = name.Split(':');
    var type = assembly.GetType("Erlang.Tests." + parts[0], true)!;
    var method = type.GetMethod(parts.Length == 1 ? "Run" : parts[1], BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
    await Task.Run(() => (Task)method.Invoke(null, null)!).WaitAsync(TimeSpan.FromSeconds(30));
    Console.WriteLine("PASS " + name);
}
