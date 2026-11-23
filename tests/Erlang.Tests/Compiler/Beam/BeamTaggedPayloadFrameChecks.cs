using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamTaggedPayloadFrameChecks
{
    internal static void Check()
    {
        const string source = """
            -module(beam_tagged_payload_frame).
            -export([dispatch/2, guarded/2]).
            dispatch(A, B) ->
                case fixture_dep:read(A, B) of
                    {chosen, Value} -> fixture_dep:effect(Value, metadata), keep(A, Value);
                    Other -> Other
                end.
            guarded(A, B) ->
                case fixture_dep:read(A, B) of
                    {chosen, Value} when is_atom(Value) -> fixture_dep:effect(Value, metadata), keep(A, Value);
                    Other -> Other
                end.
            keep(A, Value) -> {A, Value}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_tagged_payload_frame.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> frame = [Allocate, 0x20, 0x20, InitYRegisters, ExtendedListOperand, 0x10, 0x04, Move, 0x03, 0x14];
        int start = code.AsSpan().IndexOf(frame);
        ReadOnlySpan<byte> directProjection = [GetTupleElement, 0x03, 0x10, 0x04];
        ReadOnlySpan<byte> tailMoves = [Move, 0x04, 0x13, Move, 0x14, 0x03, TailLocalCallWithFrame, 0x20];
        if (start < 0 || code.AsSpan(start).IndexOf(directProjection) < 0 || code.AsSpan(start).IndexOf(tailMoves) < 0
            || code.AsSpan().IndexOf([IsAtom]) < 0)
        {
            throw new InvalidOperationException("Tagged payloads crossing a call must be projected to their stack home.");
        }

        _ = BeamModule.Prepare(beam);
    }
}
