using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalPairConstructionRootChecks
{
    internal const string TestName = "compiler/beam/keeps-a-returned-pair-live-through-terminal-construction-heap";
    private const string WrongLayout = "The closed pair did not replace the temporary stack roots safely.";

    internal static Task Run()
    {
        byte[] pair = Fixture();
        byte[] partlyRetained = Fixture(retain: true);
        if (Rewrite(pair) is not { } actual || !actual.AsSpan().SequenceEqual(Fixture(rewritten: true))
            || Rewrite(partlyRetained) is not { } mixed
            || !mixed.AsSpan().SequenceEqual(Fixture(rewritten: true, retain: true))
            || Rewrite(actual) is not null || Rewrite(pair, knownPair: false) is not null
            || Rewrite(Fixture(retain: true, clear: false)) is not null
            || Rewrite(Fixture(extraHeap: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] parent = ParentFixture();
        if (ScheduleParent(parent) is not { } scheduled
            || !scheduled.AsSpan().SequenceEqual(ParentFixture(scheduled: true))
            || ScheduleParent(scheduled) is not null
            || ScheduleParent(ParentFixture(rewritesParent: true)) is not null
            || ScheduleParent(ParentFixture(uncheckedParent: true)) is not null
            || ScheduleParent(ParentFixture(mentionsTemporary: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] ParentFixture(
        bool scheduled = false,
        bool rewritesParent = false,
        bool uncheckedParent = false,
        bool mentionsTemporary = false
    )
    {
        byte[] proved = uncheckedParent ? [] : [TestArity, F(1), X(1), U(3)];
        byte[] duplicate = rewritesParent ? [Move, X(1), Y(1)] : [];
        byte[] other = mentionsTemporary ? [Move, X(0), X(3), Move, X(3), X(0)] : [];
        byte[] child = [PutTuple, X(0), ExtendedListOperand, U(3), I(1), I(2), X(0)];
        byte[] fields = scheduled
            ?
            [
                GetTupleElement, Y(0), U(0), X(1), GetTupleElement, Y(0), U(1), X(2),
                GetTupleElement, Y(0), U(2), X(3), .. child, PutList, X(0), X(3), X(0)
            ]
            :
            [
                .. child, GetTupleElement, Y(0), U(2), X(1), PutList, X(0), X(1), X(0),
                GetTupleElement, Y(0), U(0), X(1), GetTupleElement, Y(0), U(1), X(2)
            ];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTuple, F(1), X(1), .. proved,
            Allocate, U(2), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(1), Y(1), .. duplicate, TrimFrame, U(1), U(1),
            LocalCall, U(1), F(2), TestHeap, U(10), U(1), .. fields,
            PutTuple, X(0), ExtendedListOperand, U(3), X(1), X(2), X(0), .. other,
            Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[]? ScheduleParent(byte[] code) => (byte[]?)Call(
        new BeamModuleCompiler("beam_checked_parent_tuple_probe"),
        "ScheduleCheckedParentTupleFields",
        new ReadOnlyMemory<byte>(code),
        4
    );

    private static byte[] Fixture(
        bool rewritten = false,
        bool retain = false,
        bool clear = true,
        bool extraHeap = false
    )
    {
        int width = rewritten ? (retain ? 1 : 0) : 2;
        byte head = rewritten ? (retain ? Y(0) : X(1)) : Y(1);
        byte tail = rewritten ? (retain ? X(1) : X(2)) : Y(0);
        byte[] previous = retain ? [Move, X(0), head] : [];
        byte[] initialize = retain && clear ? [InitYRegisters, ExtendedListOperand, U(1), head] : [];
        byte[] projection = rewritten
            ? [TestHeap, U(3), U(1), GetTupleElement, X(0), U(0), head, GetTupleElement, X(0), U(1), tail]
            : [
                GetTupleElement, X(0), U(0), X(1), GetTupleElement, X(0), U(1), X(0),
                Move, X(0), tail, Move, X(1), head, TestHeap, U(3), U(0)
            ];
        byte[] secondHeap = extraHeap ? [TestHeap, U(3), U(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsTuple, F(3), X(0), Allocate, U(width), U(1), .. previous, .. initialize,
            LocalCall, U(1), F(5), .. projection, .. secondHeap,
            PutTuple, X(0), ExtendedListOperand, U(2), head, tail, Deallocate, U(width), Return,
            Label, U(3), Move, A(2), X(0), Return,
            .. Start(
                4,
                5,
                2,
                1
            ),
            TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), X(0), X(0), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes, bool knownPair = true)
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
        Func<int, bool> hasPair = label => knownPair && label == 5;

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteTerminalPairConstruction",
            code,
            body,
            stack,
            hasPair
        );
    }
}
