using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamGuardedBifLeafFrameChecks
{
    internal const string TestName = "compiler/beam/guard-bif-leaf-removes-unused-frame";

    internal static Task Run()
    {
        const string source = "beam_guarded_non_gc_bif.erl";
        const string reference = "beam_guarded_non_gc_bif.beam";
        byte[] compiled = CodeGeneration.CompileBeamModule(
            Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(source)),
            source
        );
        if (!compiled.AsSpan().SequenceEqual(BeamModuleEmissionChecks.Read(reference)))
        {
            throw new InvalidOperationException(BeamGuardedBifLeafFrameDiagnostics.ExpectedOtpEncoding);
        }

        _ = BeamModule.Prepare(compiled);

        return Task.CompletedTask;
    }
}
