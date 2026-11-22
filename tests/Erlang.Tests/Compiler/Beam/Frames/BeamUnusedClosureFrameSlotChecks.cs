using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamUnusedClosureFrameSlotChecks
{
    internal const string TestName = "compiler/beam/compacts-unused-slots-in-owned-closure-frames";
    private const string LayoutError = "Expected only an unused owned closure slot to qualify.";

    internal static void Run()
    {
        HashSet<int> owned = [2];
        HashSet<int> other = [3];
        if (!ReadEntry(Body(read: false), owned).SetEquals(owned)
            || ReadEntry(Body(read: true), owned).Count != 0
            || ReadEntry(Body(read: false), other).Count != 0)
        {
            throw new InvalidOperationException(LayoutError);
        }
    }

    private static byte[] Body(bool read)
    {
        byte[] use = read ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ), Allocate, U(2), U(2),
            InitYRegisters, ExtendedListOperand, U(1), Y(0), Move, X(1), Y(1),
            LocalCall, U(1), F(2), .. use, Move, Y(1), X(0), Deallocate, U(2), Return, EndCode
        ];
    }

    private static HashSet<int> ReadEntry(byte[] bytes, HashSet<int> entries)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            3,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            null
        );

        return (HashSet<int>)Call(
            typeof(BeamModuleCompiler),
            "ReadUnusedClosureFrameEntries",
            body,
            entries
        );
    }
}
