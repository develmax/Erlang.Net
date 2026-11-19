using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamUnusedTupleBindingChecks
{
    private const string BindingMismatch = "A tuple literal binding was retained or an observable tuple match was removed.";

    internal static void Run()
    {
        const string source = """
            -module(literal_tuple_alias).
            -export([counter/0, bound/1, reused/1, effect/1]).
            counter() ->
                Args = {_Pos = 2, _Increment = 1, _Limit = 4294967295, _Reset = 0},
                ets:update_counter(alias_counter, item, Args).
            bound(Value) ->
                {Value = 1, tag}.
            reused(Value) ->
                Tuple = {_Field = 3, Value},
                {Tuple, _Field}.
            effect(Value) ->
                {_Unused = probe:value(Value), tag}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "literal_tuple_alias.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> literalCounter =
        [
            BeamEmissionFormat.Label, 0x20, BeamEmissionFormat.Move, 0x32, 0x13,
            BeamEmissionFormat.Move, 0x47, 0x00, 0x23, BeamEmissionFormat.Move, 0x42, 0x03,
            BeamEmissionFormat.Line, 0x20
        ];
        ReadOnlySpan<byte> existingBinding =
        [
            BeamEmissionFormat.ExactNotEqualTest, 0x55, 0x03, 0x13,
            BeamEmissionFormat.Line, 0x40, BeamEmissionFormat.BadMatch, 0x03
        ];
        ReadOnlySpan<byte> laterRead =
        [
            BeamEmissionFormat.Move, 0x31, 0x03, BeamEmissionFormat.Move, 0x03, 0x23,
            BeamEmissionFormat.PutTuple, 0x03, BeamEmissionFormat.ExtendedListOperand, 0x20, 0x03, 0x13,
            BeamEmissionFormat.PutTuple, 0x03, BeamEmissionFormat.ExtendedListOperand, 0x20, 0x03, 0x23
        ];
        ReadOnlySpan<byte> effectfulValue =
        [
            BeamEmissionFormat.ExternalCall, 0x10, 0x10, BeamEmissionFormat.TestHeap, 0x30, 0x10,
            BeamEmissionFormat.PutTuple, 0x03, BeamEmissionFormat.ExtendedListOperand, 0x20, 0x03, 0x82
        ];
        if (code.AsSpan().IndexOf(literalCounter) < 0 || code.AsSpan().IndexOf(existingBinding) < 0
            || code.AsSpan().IndexOf(laterRead) < 0 || code.AsSpan().IndexOf(effectfulValue) < 0)
        {
            throw new InvalidOperationException(BindingMismatch);
        }

        _ = BeamModule.Prepare(beam);
    }
}
