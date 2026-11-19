using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamGcBifCallResultChecks
{
    internal const string TestName = "compiler/beam/gc-bif-keeps-call-result-root";

    internal static Task Run()
    {
        const string source = """
            -module(gc_call_result).
            -export([remaining/1, observe/1, caught/1, increment/1]).
            remaining(Deadline) ->
                Now = probe:seconds(probe:clock()),
                Diff = Deadline - Now,
                if Diff > 0 -> Diff; true -> 0 end.
            observe(Deadline) ->
                Now = probe:seconds(probe:clock()),
                {Deadline - Now, Now}.
            caught(Deadline) ->
                try remaining(Deadline)
                catch error:badarith -> recovered
                end.
            increment(N) ->
                probe:source(N) + 1.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "gc_call_result.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> rootedResult =
        [
            BeamEmissionFormat.Allocate, 0x10, 0x10, BeamEmissionFormat.Move, 0x03, 0x04,
            BeamEmissionFormat.Line, 0x20, BeamEmissionFormat.ExternalCall, 0x00, 0x00,
            BeamEmissionFormat.ExternalCall, 0x10, 0x10, BeamEmissionFormat.Line, 0x30,
            BeamEmissionFormat.GcBifTwo, 0x05, 0x10, 0x20, 0x04, 0x03, 0x03
        ];
        ReadOnlySpan<byte> reusedInput =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x10, BeamEmissionFormat.Move, 0x03, 0x14,
            BeamEmissionFormat.Line, 0x60,
            BeamEmissionFormat.GcBifTwo, 0x05, 0x00, 0x20, 0x04, 0x14, 0x03
        ];
        if (code.AsSpan().IndexOf(rootedResult) < 0 || code.AsSpan().IndexOf(reusedInput) < 0)
        {
            throw new InvalidOperationException(BeamGcBifCallResultDiagnostics.ResultRootMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
