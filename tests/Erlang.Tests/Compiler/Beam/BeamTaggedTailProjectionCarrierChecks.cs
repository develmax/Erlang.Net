using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamTaggedTailProjectionCarrierChecks
{
    internal static Task Run()
    {
        const string source = """
            -module(beam_tagged_projection_tail_probe).
            -export([wrapped/1, retained/1]).
            wrapped(Input) ->
                case source(Input) of
                    {error, Reason} -> exit({error, {probe, Reason}});
                    Other -> Other
                end.
            retained(Input) ->
                case source(Input) of
                    {error, Reason} = Pair -> exit({Reason, Pair});
                    Other -> Other
                end.
            source(Input) -> erlang:get(Input).
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_tagged_projection_tail_probe.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> reused =
        [
            BeamEmissionFormat.TestHeap, 0x60, 0x10, BeamEmissionFormat.GetTupleElement, 0x03, 0x10, 0x03,
            BeamEmissionFormat.PutTuple
        ];
        ReadOnlySpan<byte> retained =
        [
            BeamEmissionFormat.TestHeap, 0x30, 0x10, BeamEmissionFormat.GetTupleElement, 0x03, 0x10, 0x13,
            BeamEmissionFormat.PutTuple
        ];
        int wrapped = code.AsSpan().IndexOf(reused);
        if (wrapped < 0 || code.AsSpan(wrapped + reused.Length).IndexOf(retained) < 0)
        {
            throw new InvalidOperationException("A tagged result can only surrender its register after its final read.");
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
