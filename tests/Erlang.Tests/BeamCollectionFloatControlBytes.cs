using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamCollectionFloatControlNames;

namespace Erlang.Tests;

internal static class BeamCollectionFloatControlBytes
{
    internal static byte[] AcrossCall(bool native, bool reinitialize)
    {
        byte[] invocation = native
            ? [Move, A(0), X(0), ExternalCall, U(1), U(0)]
            : [Call, U(0), F(4)];
        byte[] fresh = reinitialize ? [FloatConvert, I(3), FloatRegister, U(0)] : [];
        byte[] callee = native ? [] : [.. Start(
            3,
            4,
            3,
            0
        ), Move, I(7), X(0), Return];

        return Image(
            [.. Start(
                1,
                2,
                2,
                0
            ), Allocate, U(0), U(0),
            FloatConvert, I(2), FloatRegister, U(0),
            .. invocation,
            .. fresh,
            HeapTest, U(2), U(0), FloatMove, FloatRegister, U(0), X(0), Deallocate, U(0), Return,
            .. callee],
            labels: native ? 3 : 5,
            functions: native ? 1 : 2,
            arity: 0
        );
    }

    internal static byte[] AcrossHeap()
        => Image(
            [.. Start(
                1,
                2,
                2,
                0
            ), FloatConvert, I(2), FloatRegister, U(0),
            HeapTest, U(2), U(0), FloatMove, FloatRegister, U(0), X(0), Return],
            labels: 3,
            arity: 0
        );

    internal static byte[] Uninitialized()
        => Image(
            [.. Start(
                1,
                2,
                2,
                0
            ), HeapTest, U(2), U(0), FloatMove, FloatRegister, U(0), X(0), Return],
            labels: 3,
            arity: 0
        );
}
