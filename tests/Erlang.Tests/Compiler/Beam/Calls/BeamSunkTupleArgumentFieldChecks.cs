using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSunkTupleArgumentFieldChecks
{
    internal const string TestName = "compiler/beam/sinks-dead-tuple-fields-to-owned-call-arguments";
    private const string WrongLayout = "The outgoing field projection lost its tuple carrier or another frame root.";

    internal static Task Run()
    {
        byte[] expected = Fixture(sunk: true);
        if (Rewrite(Fixture()) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(actual) is not null || Rewrite(Fixture(changeCarrier: true)) is not null
            || Rewrite(Fixture(reuseSaved: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool sunk = false, bool changeCarrier = false, bool reuseSaved = false)
    {
        int width = sunk ? 1 : 3;
        byte retained = Y(width - 1);
        byte[] extract = sunk ? [] : [GetTupleElement, X(0), U(0), Y(0), GetTupleElement, X(0), U(1), Y(1)];
        byte[] overwrite = changeCarrier ? [Move, X(1), X(0)] : [];
        byte[] load = sunk
            ? [GetTupleElement, X(0), U(1), X(1), GetTupleElement, X(0), U(0), X(0)]
            : [Move, Y(1), X(1), Move, Y(0), X(0)];
        byte[] clear = sunk || reuseSaved ? [] : [InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)];
        byte[] returned = reuseSaved
            ? [TestHeap, U(4), U(1), PutTuple, X(0), ExtendedListOperand, U(3), X(0), Y(0), Y(1)]
            : [TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), X(0), retained];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(3), X(0), U(2), A(1),
            Allocate, U(width), U(2), Move, X(1), retained, .. extract, .. overwrite,
            .. load, .. clear, Line, U(1), LocalCall, U(2), F(5),
            .. returned, Deallocate, U(width), Return,
            Label, U(3), Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(0), Y(1), TrimFrame, U(1), U(1), Move, Y(0), X(0), Deallocate, U(1), Return,
            .. Start(
                4,
                5,
                2,
                2
            ),
            Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            6,
            2,
            code
        );
        var stack = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordStack = (start, end, slot) => stack.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            recordStack
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteOwnedTupleArgumentFields",
            code,
            body,
            stack
        );
    }
}
