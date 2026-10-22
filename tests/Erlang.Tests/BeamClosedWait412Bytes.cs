using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamClosedWait412Names;

namespace Erlang.Tests;

internal static class BeamClosedWait412Bytes
{
    internal static byte[] Image(BeamClosedWait412Shape shape)
    {
        byte[] accepted = shape switch
        {
            BeamClosedWait412Shape.MutualCycle => [CallOnly, U(1), F(LeafEntry)],
            BeamClosedWait412Shape.Return => [Return],
            BeamClosedWait412Shape.CaughtReturn => [Allocate, U(1), U(1), Try, Y(0), F(LeafInfo), ExternalCall, U(1), U(0),
                Label, U(LeafInfo), TryCase, Y(0), Move, A(MarkerAtom), X(0), Deallocate, U(1), Return],
            _ => [ExternalTail, U(1), U(0)]
        };
        byte[] caller = [.. Start(
            RunInfo,
            RunEntry,
            RunAtom,
            1
        ),
            Allocate, U(1), U(1), Try, Y(0), F(Handler), Call, U(1), F(MiddleEntry),
            Label, U(Handler), TryCase, Y(0), Move, A(MarkerAtom), X(0), Deallocate, U(1), Return];
        byte[] prefix = shape == BeamClosedWait412Shape.UninitializedRoot ? [Allocate, U(1), U(1)] : [];
        byte[] middle = [.. Start(
            MiddleInfo,
            MiddleEntry,
            MiddleAtom,
            1
        ), .. prefix,
            Label, U(Scan), Loop, F(Empty), X(0), EqualOpcode, F(Rejected), X(0), A(MarkerAtom), Remove,
            .. accepted, Label, U(Rejected), LoopEnd, F(Scan), Label, U(Empty), Wait, F(Scan)];
        byte[] leaf = shape == BeamClosedWait412Shape.MutualCycle
            ? [.. Start(
                LeafInfo,
                LeafEntry,
                LeafAtom,
                1
            ), CallOnly, U(1), F(MiddleEntry)] : [];

        return BeamOpcodeEdgeBytes.Image(
            [.. caller, .. middle, .. leaf],
            labels: 11,
            functions: shape == BeamClosedWait412Shape.MutualCycle ? 3 : 2,
            arity: 1,
            importFunction: shape == BeamClosedWait412Shape.UnknownTail ? LengthFunction : ErrorFunction
        );
    }
}
