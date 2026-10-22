using Erlang.Differential;

namespace Erlang.Tests;

internal static class BinarySearchCompiledHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(BinarySearchCompiledHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BinarySearchHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), BinarySearchHostNames.Fixture);
        Term actual = await CompiledModuleExecution.Run(artifact);
        BinarySearchMfaContracts.Require(actual.Equals(Term.Tuple(Term.A(BinarySearchHostNames.Success), Term.A(BinarySearchHostNames.Done))));
    }
}
