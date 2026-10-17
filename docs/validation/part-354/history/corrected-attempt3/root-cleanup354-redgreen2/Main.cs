global using Erlang;
using System.Security.Cryptography;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

if (args.Length != 2 || File.Exists(args[1]))
    throw new ArgumentException("Expected runtime directory and fresh outcome path");
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    string path = Path.Combine(args[0], name.Name + ".dll");

    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
if (await Console.In.ReadLineAsync() != "RUN")
    throw new InvalidOperationException("Owned job admission required");
Exception? failure = null;
try
{
    var method = Assembly.GetExecutingAssembly().GetType("Erlang.Tests.CleanupStealingHostChecks", true)!.GetMethod("Run")!;
    await (Task)method.Invoke(null, null)!;
}
catch (Exception error)
{
    failure = error is TargetInvocationException { InnerException: { } inner } ? inner : error;
}
var aggregate = failure as AggregateException;
using (var output = new FileStream(args[1], FileMode.CreateNew))
    await JsonSerializer.SerializeAsync(output, new
    {
        Passed = failure is null,
        Failure = failure?.ToString(),
        LoadedRuntime = AssemblyLoadContext.Default.Assemblies.Where(a => new[] { "Erlang.Runtime", "Erlang.Terms", "Erlang.Serialization" }.Contains(a.GetName().Name)).Select(a => new { Path = a.Location, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(a.Location))) }).ToArray(),
        InnerCount = aggregate?.InnerExceptions.Count,
        InnerType = aggregate?.InnerExceptions.FirstOrDefault()?.GetType().FullName
    });
Console.WriteLine(failure is null ? "CONTROL_RETURNED_AFTER_CLEANUP" : failure.ToString());

return failure is null ? 0 : 1;
