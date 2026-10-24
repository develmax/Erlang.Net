using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamFrameJoin394Names;

namespace Erlang.Tests;

internal static class BeamFrameJoin394Bytes
{
    internal static byte[] Join(
        bool uninitialized = false,
        bool invalidY = false,
        bool returning = false,
        bool exceptional = false
    )
    {
        byte[] terminal = returning ? [Deallocate, U(1), Return] : [BadMatch, invalidY ? Y(1) : X(0)];
        byte[] call = exceptional ? [Move, X(0), X(1), Apply, U(0)] : [];

        return Image([
            .. Start(
                1,
                2,
                2,
                1
            ), ExactEqualOpcode, F(3), X(0), A(7),
            Allocate, U(1), U(1), Move, X(0), Y(0), .. call, Jump, F(4),
            Label, U(3), Allocate, U(2), U(1), Move, X(0), Y(0),
            .. (uninitialized ? Array.Empty<byte>() : new byte[] { Move, X(0), Y(1) }),
            .. call, Label, U(4), .. terminal
        ]);
    }

    internal static byte[] ProtectedJoin(bool protectedFirst)
    {
        byte[] ordinary = [Allocate, U(1), U(1), Move, X(0), Y(0)];
        byte[] protectedArm = [Allocate, U(2), U(1), Move, X(0), Y(0), Catch, Y(1), F(5)];

        return Image(
            [
            .. Start(
                1,
                2,
                2,
                1
            ), ExactEqualOpcode, F(3), X(0), A(7),
            .. (protectedFirst ? protectedArm : ordinary), Jump, F(4),
            Label, U(3), .. (protectedFirst ? ordinary : protectedArm), Jump, F(4),
            Label, U(4), BadMatch, X(0),
            Label, U(5), CatchEnd, Y(1), Deallocate, U(2), Return
        ],
            labels: 6
        );
    }

    internal static byte[] ProtectedArm()
        => Image(
            [
            .. Start(
                1,
                2,
                2,
                1
            ), Allocate, U(2), U(1), Move, X(0), Y(0),
            Catch, Y(1), F(3), BadMatch, X(0),
            Label, U(3), CatchEnd, Y(1), Deallocate, U(2), Return
        ],
            labels: 4
        );

    internal static byte[] Last(string function, int deallocate)
        => Image([
            .. Start(
                1,
                2,
                2,
                1
            ), Allocate, U(1), U(1), Move, X(0), Y(0),
            ExternalLast, U(1), U(0), U(deallocate)
        ], importFunction: function);
}
