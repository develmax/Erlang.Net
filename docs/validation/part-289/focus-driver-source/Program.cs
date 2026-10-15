using System.Reflection;
using System.Runtime.Loader;
var path = Path.GetFullPath(args[0]);
var resolver = new AssemblyDependencyResolver(path);
AssemblyLoadContext.Default.Resolving += (context, name) => resolver.ResolveAssemblyToPath(name) is { } dependency ? context.LoadFromAssemblyPath(dependency) : null;
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
foreach (var name in args.Skip(1))
{
    bool compiledCase = name.StartsWith("erlang-", StringComparison.Ordinal);
    var type = assembly.GetType("Erlang.Tests." + (compiledCase ? "FunctionalMfaContracts" : name), true)!;
    object[]? parameters = compiledCase ? new object[] { name }
        : name == "DistributionMfaSmokeContracts" ? new object[] { "erlang", "dist_ctrl_put_data", 2 } : null;
    var method = type.GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
    int timeoutSeconds = name == "HaltSubprocessHostChecks"
        ? (int)assembly.GetType("Erlang.Tests.HaltSubprocessHostNames", true)!.GetField("OuterHostBudgetSeconds", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.GetRawConstantValue()!
        : 30;
    await Task.Run(() => (Task)method.Invoke(null, parameters)!).WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
    Console.WriteLine("PASS " + name);
}
