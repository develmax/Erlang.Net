using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProplistContinuationHostChecks
{
    internal static Task Ast()
    {
        var module = new Parser(ProplistFamilyTestData.Read(ProplistFamilyTestNames.SafetyResource)).ParseModule();
        var results = new List<ProplistContinuationSafetyResult>();
        ProplistContinuationSafetyChecks.Ast(module, results);
        Require(results, ProplistFamilyTestNames.ExpectedAstChecks);

        return Task.CompletedTask;
    }

    internal static async Task Runtime()
    {
        using var compiled = GeneratedModuleCompiler.Compile(ProplistFamilyTestData.Read(ProplistFamilyTestNames.SafetyResource), ProplistContinuationSafetyNames.SourceFile);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        var results = new List<ProplistContinuationSafetyResult>();
        await owner.InvokeAsync(async context =>
        {
            await ProplistContinuationSafetyChecks.Runtime(compiled.Definition, context, results);

            return Nil.Value;
        }).WaitAsync(ProplistFamilyTestNames.Deadline);
        Require(results, ProplistFamilyTestNames.ExpectedRuntimeChecks);
    }

    private static void Require(List<ProplistContinuationSafetyResult> results, int expected)
    {
        if (results.Count != expected)
        {
            throw new InvalidOperationException(ProplistFamilyTestNames.Failure);
        }
        var failed = results.Where(row => !row.Passed).Select(row => new InvalidOperationException(row.Name + row.Error)).ToArray();
        if (failed.Length != 0)
        {
            throw new AggregateException(ProplistFamilyTestNames.Failure, failed);
        }
    }
}
