using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamEntryArgumentClobberChecks
{
    internal const string TestName = "compiler/beam/sinks-an-independent-argument-clobber-past-a-reload";
    private const string MissingMotion = "The independent source clobber must follow the direct argument copy.";
    private const string UnsafeMotion = "An intervening read, changed source or live stack value must prevent copy motion.";
    private const string ArgumentPair = "The first call must read the old X1 before replacing its second argument.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Four = 0x40;
    private const byte Five = 0x50;
    private const byte Target = 0x35;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte X3 = 0x33;
    private const byte X4 = 0x43;
    private const byte X5 = 0x53;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;

    internal static Task Run()
    {
        byte[] entry = [Label, One, Allocate, Two, Five];
        byte[] spills = [Move, X2, Y0, Move, X1, Y1];
        byte[] clobber = [Move, X4, X2];
        byte[] preserveTarget = [Move, X3, X1, Move, X0, X3];
        byte[] reload = [Move, Y0, X0];
        byte[] call = [Line, One, LocalCall, Four, Target];
        byte[] finish = [Move, Y1, X0, Deallocate, Two, Return, EndCode];
        byte[] candidate = [.. entry, .. spills, .. clobber, .. preserveTarget, .. reload, .. call, .. finish];
        byte[] expected =
        [
            .. entry, InitYRegisters, ExtendedListOperand, One, Y0, Move, X1, Y1,
            .. preserveTarget, Move, X2, X0, .. clobber, .. call, .. finish
        ];
        if (Rewrite(candidate) is not { } result || !result.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingMotion);
        }

        byte[] readsClobber =
        [
            .. entry, .. spills, .. clobber, Move, X2, X5,
            .. preserveTarget, .. reload, .. call, .. finish
        ];
        byte[] changesSource =
        [
            .. entry, .. spills, .. clobber, Move, X0, X4,
            .. preserveTarget, .. reload, .. call, .. finish
        ];
        byte[] readsSlotAfterCall =
        [
            .. entry, .. spills, .. clobber, .. preserveTarget, .. reload, .. call,
            Move, Y0, X0, Deallocate, Two, Return, EndCode
        ];
        if (Rewrite(readsClobber) is not null || Rewrite(changesSource) is not null
            || Rewrite(readsSlotAfterCall) is not null)
        {
            throw new InvalidOperationException(UnsafeMotion);
        }

        VerifyDependentEntryCall();

        return Task.CompletedTask;
    }

    private static void VerifyDependentEntryCall()
    {
        byte[] entry =
        [
            .. Start(
                1,
                2,
                1,
                4
            ),
            Allocate, U(2), U(4)
        ];
        byte[] saved = [Move, X(1), Y(0), Move, X(3), Y(1)];
        byte[] before = [Move, Y(1), X(1), Move, Y(0), X(0)];
        byte[] after = [Move, X(1), X(0), Move, X(3), X(1)];
        byte[] tail =
        [
            Line, U(1), LocalCall, U(2), F(2), Move, Y(0), X(1), Move, Y(1), X(0),
            Deallocate, U(2), Return, EndCode
        ];
        byte[] original = [.. entry, .. saved, .. before, .. tail];
        byte[] expected = [.. entry, .. saved, .. after, .. tail];
        var (code, body, slots) = Decode(original);
        if (Call(
            typeof(BeamModuleCompiler),
            "EliminateEntryArgumentCopies",
            code,
            body,
            slots,
            false,
            false
        ) is not byte[] actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(ArgumentPair);
        }

        VerifyDeclined([.. entry, Move, X(1), Y(0), Move, X(0), Y(1), .. before, .. tail]);
        VerifyDeclined([.. entry, .. saved, InitYRegisters, ExtendedListOperand, U(1), Y(0), .. before, .. tail]);
        VerifyDeclined([.. entry, .. saved, TestHeap, U(2), U(0), .. before, .. tail]);

        static void VerifyDeclined(byte[] source)
        {
            var (_, decoded, _) = Decode(source);
            object[] instructions = ((IEnumerable)decoded).Cast<object>().ToArray();
            int allocation = Array.FindIndex(instructions, item => Opcode(item) == Allocate);
            int call = Array.FindIndex(instructions, item => Opcode(item) == LocalCall);
            if (Call(
                typeof(BeamModuleCompiler),
                "ReadDependentEntryCallCopies",
                decoded,
                allocation,
                call,
                instructions.Length
            ) is not null)
            {
                throw new InvalidOperationException(ArgumentPair);
            }
        }

        static (ReadOnlyMemory<byte> Code, object Body, List<(int Start, int End, int Slot)> Slots) Decode(byte[] source)
        {
            var code = new ReadOnlyMemory<byte>(source);
            object header = New(
                "BeamCodeHeader",
                0,
                (int)InitYRegisters,
                4,
                1,
                code
            );
            var slots = new List<(int Start, int End, int Slot)>();
            Action<int, int, int> record = (start, end, slot) => slots.Add((start, end, slot));
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                3,
                0,
                null,
                record
            );

            return (code, body, slots);
        }

        static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
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

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "SinkEntryArgumentClobbers",
            code,
            body
        );
    }
}
