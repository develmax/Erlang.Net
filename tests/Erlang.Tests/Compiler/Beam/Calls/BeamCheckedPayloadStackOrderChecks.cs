using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedPayloadStackOrderChecks
{
    private const int SyntheticAtomCount = 2;
    internal const string TestName = "compiler/beam/orders-checked-payload-stack-loads-before-the-clear";

    internal static Task Run()
    {
        byte[] expected = Fixture(sorted: true);
        if (Rewrite(Fixture()) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite(Fixture(clearReceiver: true)) is not null
            || Rewrite(Fixture(otherTuple: true)) is not null)
        {
            throw new InvalidOperationException(
                "Only independent loads before the checked receiver and its frame clear can change order."
            );
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool sorted = false, bool clearReceiver = false, bool otherTuple = false)
    {
        byte[] loads = sorted ? [Move, Y(3), X(2), Move, Y(4), X(1)] : [Move, Y(4), X(1), Move, Y(3), X(2)];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Allocate, U(5), U(3),
            InitYRegisters, ExtendedListOperand, U(3), Y(0), Y(1), Y(2),
            Move, X(2), Y(3), Move, X(1), Y(4), ExternalCall, U(1), U(0),
            Move, X(0), Y(2), IsTaggedTuple, F(3), X(0), U(2), A(1),
            GetTupleElement, otherTuple ? Y(2) : X(0), U(1), Y(1), .. loads,
            InitYRegisters, ExtendedListOperand, U(2), clearReceiver ? Y(1) : Y(3), Y(4),
            Move, Y(1), X(0), Line, U(1), ExternalCall, U(3), U(1),
            Label, U(3), Deallocate, U(5), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            4,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            SyntheticAtomCount,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "OrderCheckedPayloadStackArguments",
            code,
            body
        );
    }
}
