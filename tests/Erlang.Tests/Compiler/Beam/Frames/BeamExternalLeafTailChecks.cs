using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamExternalLeafTailChecks
{
    internal const string TestName = "compiler/beam/leaf-external-tail-removes-unused-frame";

    internal static Task Run()
    {
        const string source = "beam_external_leaf_tail.erl";
        const string reference = "beam_external_leaf_tail.beam";
        byte[] compiled = CodeGeneration.CompileBeamModule(
            Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(source)),
            source
        );
        if (!compiled.AsSpan().SequenceEqual(BeamModuleEmissionChecks.Read(reference)))
        {
            throw new InvalidOperationException(BeamExternalLeafTailDiagnostics.ExpectedOtpEncoding);
        }

        _ = BeamModule.Prepare(compiled);

        return Task.CompletedTask;
    }
}
