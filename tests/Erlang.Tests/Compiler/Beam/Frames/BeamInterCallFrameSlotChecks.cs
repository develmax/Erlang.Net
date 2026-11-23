using System.Buffers.Binary;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamInterCallFrameSlotChecks
{
    internal const string TestName = "compiler/beam/promotes-a-lower-slot-between-ordinary-calls";
    private const string WrongCode = "Expected one lower frame slot to be replaced without changing either call, line or guarded fallback.";
    private const string UnsafeCode = "A shared block, collection, call clobber, extra definition or trim must keep the stack slot.";
    private const string WrongValue = "The promoted result, saved argument or guarded error changed.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Failed = 0x35;
    private const byte Seed = 0x55;
    private const byte Touch = 0x75;
    private const byte Marker = 0x72;
    private const byte Fallback = 0x82;

    internal static async Task Run()
    {
        byte[] original = Program(Two, Y0, Y1);
        byte[] retainedScratch = Program(
            Two,
            Y0,
            Y1,
            readsOriginalX1: true
        );
        byte[] expected = Program(One, X1, Y0);
        byte[] expectedWithRetainedScratch = Program(
            One,
            X2,
            Y0,
            readsOriginalX1: true
        );
        if (Rewrite(original) is not { } promoted || !promoted.AsSpan().SequenceEqual(expected)
            || Rewrite(retainedScratch) is not { } promotedScratch
            || !promotedScratch.AsSpan().SequenceEqual(expectedWithRetainedScratch))
        {
            throw new InvalidOperationException(WrongCode);
        }

        byte[][] obstacles =
        [
            [TestHeap, Three, One],
            [LocalCall, One, Touch],
            [Label, 0x80],
            [Move, X0, Y0],
            [TrimFrame, One, One]
        ];
        if (obstacles.Any(instructions => Rewrite(Program(
            Two,
            Y0,
            Y1,
            obstacle: instructions
        )) is not null))
        {
            throw new InvalidOperationException(UnsafeCode);
        }

        BeamModule baseline = Load(original);
        BeamModule candidate = Load(promoted);
        BeamModule scratchBaseline = Load(retainedScratch);
        BeamModule scratchCandidate = Load(promotedScratch);
        await using var runtime = new ProcessRuntime();
        Term accepted = Term.A("chosen");
        Term rejected = Term.I(42);
        Term retained = Term.Tuple(Term.A("keep"), Term.List(Term.I(1), Term.I(2)));
        Term wrappedFailure = Term.Tuple(Term.A(BeamOpcodeEdgeNames.Marker), rejected);
        Term retainedFailure = Term.A(BeamOpcodeEdgeNames.Other);
        ProcessHandle process = runtime.Spawn(async context =>
        {
            foreach (BeamModule image in new[] { baseline, candidate, scratchBaseline, scratchCandidate })
            {
                Term result = await image.Invoke(
                    context,
                    "run",
                    accepted,
                    retained
                );
                if (!Term.Tuple(accepted, retained).Equals(result))
                {
                    throw new InvalidOperationException(WrongValue);
                }

                Term expectedFailure = image == baseline || image == candidate ? wrappedFailure : retainedFailure;
                try
                {
                    _ = await image.Invoke(
                        context,
                        "run",
                        rejected,
                        retained
                    );

                    throw new InvalidOperationException(WrongValue);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), expectedFailure)))
                {
                }
            }

            return Term.A("ok");
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        if (!exit.Equals(Term.A("normal")))
        {
            throw new InvalidOperationException(WrongValue);
        }
    }

    private static byte[] Program(
        byte width,
        byte transient,
        byte saved,
        bool readsOriginalX1 = false,
        byte[]? obstacle = null
    )
    {
        byte[] initialize = width == Two ? [InitYRegisters, ExtendedListOperand, One, Y0] : [];
        byte[] preserve = readsOriginalX1 ? [Move, Fallback, X1] : [];
        byte[] entry = BeamOpcodeEdgeBytes.Start(
            1,
            2,
            2,
            2
        );
        byte[] seed = BeamOpcodeEdgeBytes.Start(
            4,
            5,
            3,
            1
        );
        byte[] touch = BeamOpcodeEdgeBytes.Start(
            6,
            7,
            4,
            1
        );

        return
        [
            .. entry, Allocate, width, Two, .. initialize, Move, X1, saved,
            LocalCall, One, Seed, .. preserve, IsTaggedTuple, Failed, X0, Two, Marker,
            GetTupleElement, X0, One, transient, .. (obstacle ?? []), IsAtom, Failed, transient, Move, transient, X0,
            LocalCall, One, Touch, TestHeap, Three, One, PutTuple, X0, ExtendedListOperand, Two, X0, saved,
            Deallocate, width, Return, Label, Three, BadMatch, readsOriginalX1 ? X1 : X0,
            .. seed, TestHeap, Three, One, PutTuple, X0, ExtendedListOperand, Two, Marker, X0, Return,
            .. touch, Return, EndCode
        ];
    }

    private static BeamModule Load(byte[] program)
    {
        byte[] image = BeamOpcodeEdgeBytes.Image(
            program[..^1],
            labels: 9,
            functions: 3,
            arity: 2
        );
        byte[] code = BeamDocumentationEmissionChecks.Chunks(image).Single(chunk => chunk.Id == "Code").Data;
        BinaryPrimitives.WriteInt32BigEndian(code.AsSpan(8), InitYRegisters);

        return BeamModule.Prepare(BeamMetadataModeledBytes.WithChunk(image, "Code", code));
    }

    private static byte[]? Rewrite(byte[] program)
    {
        ReadOnlyMemory<byte> code = program;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            9,
            3,
            code
        );
        var stackOperands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> stack = (start, end, slot) => stackOperands.Add((start, end, slot));
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            9,
            0,
            null,
            stack
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "PromoteInterCallFrameSlots",
            code,
            instructions,
            stackOperands
        );
    }
}
