using Erlang.Differential;

namespace Erlang.Tests;

internal static class BinaryInsertHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(BinaryInsertHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(BinaryInsertHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), BinaryInsertHostNames.Fixture);
        Term result = await CompiledModuleExecution.Run(artifact);
        Term expected = Term.Tuple(Term.A(BinaryInsertHostNames.Success), Term.A(BinaryInsertHostNames.Done));
        if (!result.Equals(expected))
            throw new InvalidOperationException(BinaryInsertHostNames.Failure);
    }
}
