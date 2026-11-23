using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamNestedGuardedTailChecks
{
    internal const string TestName = "compiler/beam/nested-guard-tests-before-other-projections";
    private const string Module = "beam_nested_guarded_tail";
    private const string Failure = "The guarded nested case changed its projection order, roots or tail arguments.";

    internal static Task Run()
    {
        string file = Module + ".erl";
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(file));
        byte[] image = CodeGeneration.CompileBeamModule(source, file);
        if (!image.AsSpan().SequenceEqual(BeamModuleEmissionChecks.Read(Module + ".beam")))
        {
            throw new InvalidOperationException(Failure);
        }

        _ = BeamModule.Prepare(image);

        return Task.CompletedTask;
    }
}
