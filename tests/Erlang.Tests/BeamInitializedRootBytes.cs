using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamInitializedRootNames;

namespace Erlang.Tests;

internal static class BeamInitializedRootBytes
{
    internal static byte[] Image(BeamInitializedRootShape shape)
    {
        // Synthetic admission image; no OTP source/body compatibility credit.
        // Y1 is nil after try_end, saved opaque trace after try_case.
        byte[] ending = shape switch
        {
            BeamInitializedRootShape.GcOnly => [
                HeapTest, U(0), U(0), Move, A(7), X(0), Deallocate, U(FrameSlots), Return],
            BeamInitializedRootShape.TrimGc => [
                Trim, U(1), U(1), HeapTest, U(0), U(0), Move, A(7), X(0), Deallocate, U(1), Return],
            BeamInitializedRootShape.TransportEscape => [
                HeapTest, U(0), U(0), Move, Y(MixedSlot), X(0), Deallocate, U(FrameSlots), Return],
            BeamInitializedRootShape.TupleEscape => [
                HeapTest, U(2), U(0), PutTuple, X(0), ArgumentList, U(1), Y(MixedSlot), Deallocate, U(FrameSlots), Return],
            BeamInitializedRootShape.TrimEscape => [
                Trim, U(1), U(1), HeapTest, U(0), U(0), Move, Y(0), X(0), Deallocate, U(1), Return],
            BeamInitializedRootShape.ReallocateUninitialized => [
                Deallocate, U(FrameSlots), Allocate, U(FrameSlots), U(0), Move, A(0), Y(0),
                HeapTest, U(0), U(0), Move, A(7), X(0), Deallocate, U(FrameSlots), Return],
            BeamInitializedRootShape.OverwriteThenRead => [
                Move, A(0), Y(MixedSlot), HeapTest, U(0), U(0), Move, Y(MixedSlot), X(0), Deallocate, U(FrameSlots), Return],
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };

        return BeamOpcodeEdgeBytes.Image(
            [.. Start(
                1,
                2,
                2,
                Arity
            ), Allocate, U(FrameSlots), U(Arity), Move, A(0), Y(MixedSlot),
            Try, Y(HandlerSlot), F(HandlerLabel), EqualOpcode, F(FailureLabel), X(0), A(7),
            TryEnd, Y(HandlerSlot), Jump, F(JoinLabel),
            Label, U(FailureLabel), Move, A(7), X(0), ExternalCall, U(1), U(0),
            Label, U(HandlerLabel), TryCase, Y(HandlerSlot), Move, X(2), Y(MixedSlot),
            Label, U(JoinLabel), .. ending],
            labels: LabelCount,
            arity: Arity,
            importFunction: BeamClosureProtocolNames.ErrorImport
        );
    }
}
