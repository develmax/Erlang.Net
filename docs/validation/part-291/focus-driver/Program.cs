using System.Reflection;
using System.Runtime.Loader;
var path = Path.GetFullPath(args[0]);
var resolver = new AssemblyDependencyResolver(path);
AssemblyLoadContext.Default.Resolving += (context, name) => resolver.ResolveAssemblyToPath(name) is { } dependency ? context.LoadFromAssemblyPath(dependency) : null;
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
foreach (var name in args.Skip(1))
{
    if (name.StartsWith("nif-declaration-", StringComparison.Ordinal))
    {
        var fixture = Erlang.Differential.CompiledDiagnosticCases.All.Single(f => f.Name == name);
        bool rejected = false;
        try { using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source); }
        catch (Erlang.Compiler.CompileException exception)
        {
            rejected = exception.Code == Erlang.Differential.CompiledDiagnosticExpectations.Code(fixture)
                && exception.Message == Erlang.Differential.CompiledDiagnosticExpectations.Message(fixture);
        }
        if (!rejected) throw new InvalidOperationException("Diagnostic mismatch: " + name);
        Console.WriteLine("PASS " + name);
        continue;
    }
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
