using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPostCallProjectionSlotChecks
{
    internal const string TestName = "compiler/beam/promotes-only-post-call-projections-local-to-a-terminal-arm";
    private const string FixtureFailure = "The terminal projection must retain the surviving high stack value and resize the frame.";
    private const string ControlFailure = "The projection slot must stay on the stack when it or a displaced X root survives a failure edge.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Failed = 0x25;
    private const byte Ok = 0x12;

    internal static Task Run()
    {
        byte[] prefix =
        [
            Label, One,
            Allocate, Three, Two,
            Move, X1, Y2,
            InitYRegisters, ExtendedListOperand, Two, Y0, Y1,
            ExternalCall, One, Zero,
            IsTaggedTuple, Failed, X0, Two, Ok,
            GetTupleElement, X0, One, Y0,
            IsNonemptyList, Failed, Y0,
            GetList, Y0, Y1, X0,
            IsNil, Failed, X0,
            TestHeap, Three, Zero,
            PutTuple, X1, ExtendedListOperand, Two, Y1, Y2,
            Move, Y2, X0,
            TailExternalCallWithFrame, Two, Zero, Three,
            Label, Two
        ];
        byte[] finish = [Deallocate, Three, Return, EndCode];
        byte[] source = [.. prefix, Move, Y2, X0, .. finish];

        if (Rewrite(source) is not { } optimized)
        {
            throw new InvalidOperationException(FixtureFailure);
        }

        object[] instructions = ((IEnumerable)Read(optimized)).Cast<object>().ToArray();
        object tail = instructions.Single(instruction => Opcode(instruction) == TailExternalCallWithFrame);
        object allocation = instructions.Single(instruction => Opcode(instruction) == Allocate);
        object deallocation = instructions.Single(instruction => Opcode(instruction) == Deallocate);
        object heap = instructions.Single(instruction => Opcode(instruction) == TestHeap);
        object fields = instructions.Single(instruction => Opcode(instruction) == GetList);
        object nested = instructions.Single(instruction => Opcode(instruction) == PutTuple);
        if (Value(allocation, 0) != 1 || Value(tail, 2) != 1 || Value(deallocation, 0) != 1
            || Value(heap, 1) < 1 || Kind(fields, 1) != "x" || Kind(nested, 3) != "y" || Value(nested, 3) != 0)
        {
            throw new InvalidOperationException(FixtureFailure);
        }

        byte[] readsSlot = [.. prefix, Move, Y0, X0, .. finish];
        byte[] rootsX0 = [.. prefix, TestHeap, Zero, One, Move, Y2, X0, .. finish];
        byte[] callsAgain = [.. prefix, Move, Y2, X0, ExternalCall, One, Zero, .. finish];
        if (Rewrite(readsSlot) is not null || Rewrite(rootsX0) is not null || Rewrite(callsAgain) is not null)
        {
            throw new InvalidOperationException(ControlFailure);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        var slots = new List<(int Start, int End, int Slot)>();
        object body = Read(source, (start, end, slot) => slots.Add((start, end, slot)));

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "PromotePostCallProjectionSlots",
            new ReadOnlyMemory<byte>(source),
            body,
            slots
        );
    }

    private static object Read(byte[] code, Action<int, int, int>? recordSlot = null)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            3,
            1,
            new ReadOnlyMemory<byte>(code)
        );

        return Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            recordSlot
        );
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object Operand(object instruction, int index) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index);

    private static BigInteger Value(object instruction, int index) =>
        (BigInteger)Property(Operand(instruction, index), "Value");

    private static string Kind(object instruction, int index) =>
        (string)Property(Operand(instruction, index), "Kind");
}
