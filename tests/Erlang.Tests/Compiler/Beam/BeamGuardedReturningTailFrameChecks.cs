using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamGuardedReturningTailFrameChecks
{
    internal const string TestName = "compiler/beam/promotes-guarded-tail-arguments-after-one-returning-call";
    private const string Fixture = "beam_guarded_returning_tail_frame.erl";
    private const string WrongLayout = "The guarded returning continuation lost an argument or retained its Y slots.";

    internal static Task Run()
    {
        byte[] unscheduled = Image();
        object original = Decode(unscheduled);
        object[] operations = Items(original);
        int call = Array.FindIndex(operations, operation => Opcode(operation) == ExternalCall);
        int tail = Array.FindIndex(operations, operation => Opcode(operation) == TailExternalCallWithFrame);
        int[] expected = [tail - 3, tail - 5, tail - 4, tail - 2, tail - 6];
        if (Call(
            typeof(BeamModuleCompiler),
            "FindGuardedReturningSelector",
            original,
            2,
            operations.Length - 1
        ) is not int found
            || found != call || Call(
                typeof(BeamModuleCompiler),
                "PlanGuardedReturningArgumentOrder",
                original,
                call,
                tail
            ) is not int[] order
            || !order.SequenceEqual(expected)
            || Call(
                typeof(BeamModuleCompiler),
                "PlanGuardedReturningArgumentOrder",
                Decode(Image(dependentArgument: true)),
                call,
                tail
            ) is not null
            || RewriteFrame(Image(ordered: true, liveStore: true)) is not null
            || RewriteFrame(Image(ordered: true)) is not { } rewritten)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] result = Items(Decode(rewritten));
        if (Value(Operands(result.Single(operation => Opcode(operation) == Allocate))[0]) != 0
            || Value(Operands(result.Single(operation => Opcode(operation) == TailExternalCallWithFrame))[^1]) != 0
            || result.Any(operation => Opcode(operation) == InitYRegisters
                || Operands(operation).Any(operand => (string)Property(operand, "Kind") == "y")))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(bool ordered = false, bool liveStore = false, bool dependentArgument = false)
    {
        byte[] store = liveStore ? [Move, X(0), Y(0)] : [];
        byte[] projection = liveStore ? [] : [GetTupleElement, Y(2), U(2), Y(0)];
        byte[] newX4 = dependentArgument ? [Move, X(1), X(4)] : [Move, A(3), X(4)];
        byte[] bundle = ordered
            ? [Move, Y(0), X(1), Move, Y(1), X(3), GetTupleElement, Y(2), U(4), X(2),
                GetTupleElement, Y(2), U(3), X(0), .. newX4]
            : [.. newX4, Move, Y(1), X(3), GetTupleElement, Y(2), U(4), X(2),
                Move, Y(0), X(1), GetTupleElement, Y(2), U(3), X(0)];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(3), U(1), InitYRegisters, ExtendedListOperand, U(3), Y(0), Y(1), Y(2),
            .. store, ExternalCall, U(1), U(0),
            IsTuple, F(5), X(0), TestArity, F(5), X(0), U(2),
            GetTupleElement, X(0), U(0), X(1), GetTupleElement, X(0), U(1), Y(2),
            SelectValue, X(1), F(5), ExtendedListOperand, U(2), A(3), F(3),
            Label, U(3), IsTaggedTuple, F(4), Y(2), U(5), A(4),
            GetTupleElement, Y(2), U(1), Y(1), .. projection,
            Label, U(6), .. bundle, Line, U(1), TailExternalCallWithFrame, U(5), U(1), U(3),
            Label, U(4), Move, Y(2), X(0), Deallocate, U(3), Return,
            Label, U(5), CaseEnd, X(0), EndCode
        ];
    }

    private static byte[]? RewriteFrame(byte[] bytes)
    {
        var state = new BeamModuleCompiler(Fixture);
        var operands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> readY = (first, last, slot) => operands.Add((first, last, slot));

        return (byte[]?)Call(
            state,
            "ReallocateLeafFrameRegistersCore",
            new ReadOnlyMemory<byte>(bytes),
            Decode(bytes, readY),
            operands,
            true,
            null,
            null,
            false,
            null,
            null,
            false,
            new HashSet<int> { 2 }
        );
    }

    private static object Decode(byte[] bytes, Action<int, int, int>? readY = null)
    {
        var code = new ReadOnlyMemory<byte>(bytes);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            7,
            1,
            code
        );

        return Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            null,
            readY
        );
    }

    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Operands(object instruction) => Items(Property(instruction, "Operands"));

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
