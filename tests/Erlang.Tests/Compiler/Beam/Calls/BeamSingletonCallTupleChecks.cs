using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamSingletonCallTupleChecks
{
    internal const string TestName = "compiler/beam/singleton-return-tuple-keeps-calls";

    internal static Task Run()
    {
        const string source = "singleton_return_probe.erl";
        const string reference = "singleton_return_probe.beam";
        byte[] candidate = CodeGeneration.CompileBeamModule(
            Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(source)),
            source
        );
        if (!candidate.AsSpan().SequenceEqual(BeamModuleEmissionChecks.Read(reference)))
        {
            throw new InvalidOperationException(BeamSingletonCallTupleDiagnostics.ExpectedOtpEncoding);
        }

        _ = BeamModule.Prepare(candidate);

        return Task.CompletedTask;
    }
}
