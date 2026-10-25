using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamCollectionFloatControlNames;

namespace Erlang.Tests;

internal static class BeamFloatStore479Bytes
{
    internal static byte[] Store(
        bool trim = false,
        bool gcBeforeFilled = false,
        bool readBeforeFilled = false,
        bool invalidFloat = false,
        bool invalidDestination = false,
        bool overwriteHandler = false
    )
    {
        byte[] handler = overwriteHandler ? [Move, I(0), Y(0), BeamFloatStore479Names.Catch, Y(1), F(3)] : [];
        byte[] beforeFilled = gcBeforeFilled ? [HeapTest, U(2), U(0)]
            : readBeforeFilled ? [Move, Y(0), X(0)] : [];
        byte[] finish = trim
            ? [BeamFloatStore479Names.Trim, U(1), U(1), HeapTest, U(2), U(0), Move, Y(0), X(0), Deallocate, U(1), Return]
            : [FloatMove, FloatRegister, U(0), Y(0), HeapTest, U(2), U(0), Move, Y(0), X(0), Deallocate, U(2), Return];
        byte[] landing = overwriteHandler
            ? [BeamFloatStore479Names.Label, U(3), BeamFloatStore479Names.CatchEnd, Y(1), Move, I(0), X(0), Deallocate, U(2), Return] : [];

        return Image(
            [.. Start(
                1,
                2,
                2,
                0
            ), FloatConvert, I(2), FloatRegister, U(0),
            HeapTest, U(4), U(0), Allocate, U(2), U(0), .. handler,
            FloatMove, FloatRegister, U(invalidFloat ? 1 : 0), Y(invalidDestination ? 2 : 1),
            .. beforeFilled, .. finish, .. landing],
            labels: overwriteHandler ? 4 : 3,
            arity: 0
        );
    }
}
