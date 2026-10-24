using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamInfallibleBifControlNames;

namespace Erlang.Tests;

internal static class BeamInfallibleBifControlBytes
{
    internal static byte[] Image(
        string function,
        int arity,
        bool gc = false,
        bool opaque = false,
        bool missingArgument = false
    )
    {
        byte source = opaque ? Y(1) : missingArgument ? X(arity) : X(0);
        byte[] call = arity == 1
            ? gc ? [UnaryGc, F(0), U(arity), U(0), source, Y(0)] : [Unary, F(0), U(0), source, Y(0)]
            : [Binary, F(0), U(0), source, X(1), Y(0)];
        byte[] install = [Catch, Y(1), F(3)];

        // No init_yregs here on purpose: the admitted infallible BIF initializes
        // Y0 before a real handler tag occupies Y1. No GC may occur beforehand.
        return BeamOpcodeEdgeBytes.Image(
            [
            .. Start(
                1,
                2,
                2,
                arity
            ),Allocate,U(2),U(arity),
            .. (opaque ? install:[]),.. call,.. (opaque ? Array.Empty<byte>():install),
            Move,Y(0),X(0),CatchEnd,Y(1),Deallocate,U(2),Return,
            Label,U(3),CatchEnd,Y(1),Deallocate,U(2),Return
        ],
            labels: 4,
            arity: arity,
            importFunction: function,
            importArity: arity
        );
    }

    internal static byte[] Remote(string function, int arity)
        => BeamOpcodeEdgeBytes.Image(
            [.. Start(
                1,
                2,
                2,
                arity
            ),ExternalTail,U(arity),U(0)],
            labels: 3,
            arity: arity,
            importFunction: function,
            importArity: arity
        );
}
