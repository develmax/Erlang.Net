using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamAdjacentBifResultChecks
{
    internal static Task Run()
    {
        const string source = """
            -module(bif_result_chain).
            -export([time/1, observed/1]).
            time(T) ->
                Utc = calendar:now_to_universal_time(T),
                Seconds = calendar:datetime_to_gregorian_seconds(Utc) - 62105714048,
                Seconds band 4294967295.
            observed(T) ->
                Utc = calendar:now_to_universal_time(T),
                Seconds = calendar:datetime_to_gregorian_seconds(Utc) - 62105714048,
                {Seconds band 4294967295, Seconds}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "bif_result_chain.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> rootedResult =
        [
            BeamEmissionFormat.Allocate, 0x00, 0x10,
            BeamEmissionFormat.Line, 0x20, BeamEmissionFormat.ExternalCall, 0x10, 0x00,
            BeamEmissionFormat.Line, 0x30, BeamEmissionFormat.ExternalCall, 0x10, 0x10,
            BeamEmissionFormat.GcBifTwo, 0x05, 0x10, 0x20, 0x03, 0x79, 0x0E, 0x75, 0xC9, 0xFD, 0x80, 0x03,
            BeamEmissionFormat.Line, 0x40,
            BeamEmissionFormat.GcBifTwo, 0x05, 0x10, 0x30,
            BeamEmissionFormat.ExtendedTypedRegisterOperand, 0x03, 0x10, 0x79, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x03,
            BeamEmissionFormat.Deallocate, 0x00, BeamEmissionFormat.Return
        ];
        ReadOnlySpan<byte> laterRead =
        [
            BeamEmissionFormat.GcBifTwo, 0x05, 0x10, 0x20, 0x03, 0x79, 0x0E, 0x75, 0xC9, 0xFD, 0x80, 0x03,
            BeamEmissionFormat.Move, 0x03, 0x04, BeamEmissionFormat.Line, 0x80,
            BeamEmissionFormat.GcBifTwo, 0x05, 0x00, 0x30, 0x04, 0x79, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x03
        ];
        if (code.AsSpan().IndexOf(rootedResult) < 0 || code.AsSpan().IndexOf(laterRead) < 0)
        {
            throw new InvalidOperationException(BeamAdjacentBifResultDiagnostics.RootMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
