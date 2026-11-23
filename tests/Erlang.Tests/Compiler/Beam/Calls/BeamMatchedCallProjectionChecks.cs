using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamMatchedCallProjectionChecks
{
    internal static Task Run()
    {
        const string source = """
            -module(matched_call_result).
            -export([project/2, keep/2, produce/1]).
            project(Input, Name) ->
                Children = ?MODULE:produce(Input),
                case lists:keysearch(Name, 1, Children) of
                    {value, {_, Selected, _, _}} -> {ok, Selected};
                    false -> missing
                end.
            keep(Input, Name) ->
                Children = ?MODULE:produce(Input),
                case lists:keysearch(Name, 1, Children) of
                    {value, {_, Selected, _, _}} -> {ok, Selected};
                    false -> {missing, Name}
                end.
            produce(V) -> V.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "matched_call_result.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> deadSlot =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x00,
            BeamEmissionFormat.Move, 0x11, 0x13, BeamEmissionFormat.Move, 0x03, 0x23,
            BeamEmissionFormat.Move, 0x04, 0x03, BeamEmissionFormat.InitYRegisters, 0x17, 0x10, 0x04,
            BeamEmissionFormat.Line, 0x30, BeamEmissionFormat.ExternalCall, 0x30, 0x10
        ];
        ReadOnlySpan<byte> projected =
        [
            BeamEmissionFormat.TestArity, 0x45, 0x13, 0x40,
            BeamEmissionFormat.TestHeap, 0x30, 0x20, BeamEmissionFormat.GetTupleElement, 0x13, 0x10, 0x03,
            BeamEmissionFormat.PutTuple, 0x03, 0x17, 0x20, 0x72, 0x03
        ];
        ReadOnlySpan<byte> liveSlot =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x00,
            BeamEmissionFormat.Move, 0x11, 0x13, BeamEmissionFormat.Move, 0x03, 0x23,
            BeamEmissionFormat.Move, 0x04, 0x03, BeamEmissionFormat.Line, 0x60,
            BeamEmissionFormat.ExternalCall, 0x30, 0x10
        ];
        if (code.AsSpan().IndexOf(deadSlot) < 0
            || code.AsSpan().IndexOf(projected) < 0
            || code.AsSpan().IndexOf(liveSlot) < 0)
        {
            throw new InvalidOperationException(BeamMatchedCallProjectionDiagnostics.ProjectionMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
