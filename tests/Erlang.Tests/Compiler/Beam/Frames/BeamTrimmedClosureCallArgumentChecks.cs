using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTrimmedClosureCallArgumentChecks
{
    private const string Pass = "ScheduleTrimmedClosureCallArguments";
    private const string Arguments = "Expected direct closure output and a literal argument placed after the trim.";
    private const string FrameOrder = "Compaction must keep the same values across the closure and return trims.";
    private const string LiveSlots = "The closure corridor must keep saved values used on either side of the trim.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("trimmed_closure_call_argument_fixture");
        _ = Call(compiler, "LiteralOperand", new Cons(Term.I(7), Nil.Value));
        _ = Call(compiler, "Intern", BeamOpcodeEdgeNames.Run);
        byte[] original = Fixture();
        if (Call(
            compiler,
            Pass,
            new ReadOnlyMemory<byte>(original),
            5
        ) is not byte[] rewritten
            || rewritten.AsSpan().IndexOf(new byte[] { Allocate, U(2), U(2) }) < 0
            || rewritten.AsSpan().IndexOf(new byte[] { MakeFun, U(0), X(1), ExtendedListOperand, U(1) }) < 0
            || rewritten.AsSpan().IndexOf(new byte[] { Move, ExtendedLiteralOperand, U(0), X(0), Line, U(2), ExternalCall, U(2), U(0) }) < 0)
        {
            throw new InvalidOperationException(Arguments);
        }

        foreach (byte[] control in new[] { Fixture(captureValue: true), Fixture(readAfterCall: true) })
        {
            if (Call(
                compiler,
                Pass,
                new ReadOnlyMemory<byte>(control),
                5
            ) is not null)
            {
                throw new InvalidOperationException(LiveSlots);
            }
        }

        if (Call(
            compiler,
            Pass,
            new ReadOnlyMemory<byte>(Fixture(relocate: false)),
            5
        ) is not byte[] withoutRelocation
            || withoutRelocation.AsSpan().IndexOf(new byte[] { MakeFun, U(0), X(1), ExtendedListOperand, U(1) }) < 0
            || withoutRelocation.AsSpan().IndexOf(new byte[] { Move, ExtendedLiteralOperand, U(0), X(0), Line, U(2), ExternalCall, U(2), U(0) }) < 0
            || Call(
                compiler,
                Pass,
                new ReadOnlyMemory<byte>(Fixture(relocate: false, readClosure: true)),
                5
            ) is not null)
        {
            throw new InvalidOperationException(LiveSlots);
        }

        IReadOnlySet<int> entries = new HashSet<int> { 2 };
        if (OrderFrame(compiler, FrameFixture(), entries) is not { } ordered
            || !ordered.SequenceEqual(FrameFixture(ordered: true))
            || OrderFrame(compiler, FrameFixture(interiorEntry: true), entries) is not null)
        {
            throw new InvalidOperationException(FrameOrder);
        }

        if (Call(
            compiler,
            "OrderSoleRetainedClosureFrame",
            new ReadOnlyMemory<byte>(SoleFrameFixture()),
            5
        ) is not byte[] soleFrame || !soleFrame.SequenceEqual(SoleFrameFixture(ordered: true))
            || Call(
                compiler,
                "OrderSoleRetainedClosureFrame",
                new ReadOnlyMemory<byte>(SoleFrameFixture(interiorEntry: true)),
                5
            ) is not null)
        {
            throw new InvalidOperationException(FrameOrder);
        }

        if (ApplyEntryOrder(compiler) is not { } entryOrdered
            || !entryOrdered.SequenceEqual(EntryCallbackFixture(ordered: true))
            || ApplyEntryOrder(compiler, capture: true) is not null)
        {
            throw new InvalidOperationException(FrameOrder);
        }

        return Task.CompletedTask;
    }

    private static byte[]? ApplyEntryOrder(BeamModuleCompiler compiler, bool capture = false) => (byte[]?)Call(
        compiler,
        "OrderEntryCallbackOperandFrame",
        new ReadOnlyMemory<byte>(EntryCallbackFixture(capture: capture)),
        5
    );

    private static byte[] EntryCallbackFixture(bool ordered = false, bool capture = false)
    {
        byte first = ordered ? Y(1) : Y(0);
        byte second = ordered ? Y(0) : Y(1);
        byte[] saves = ordered ? [Move, X(1), second, Move, X(0), first]
            : [Move, X(0), first, Move, X(1), second];
        byte[] captureList = capture ? [U(1), second] : [U(0)];
        byte[] reload = ordered ? [] : [Move, second, X(1)];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            AllocateHeap, U(3), U(0), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(2), .. saves,
            MakeFun, U(0), X(0), ExtendedListOperand, .. captureList, .. reload, LocalCall, U(2), F(2),
            Move, X(0), X(1), Move, first, X(0), LocalCall, U(2), F(2),
            Move, X(0), Y(2), TestHeap, U(0), U(0), MakeFun, U(1), X(0), ExtendedListOperand, U(0),
            Move, second, X(1), LocalCall, U(2), F(2),
            InitYRegisters, ExtendedListOperand, U(2), ordered ? second : first, ordered ? first : second,
            Move, Y(2), X(0), Deallocate, U(3), Return, EndCode
        ];
    }

    private static byte[]? OrderFrame(BeamModuleCompiler compiler, byte[] bytes, IReadOnlySet<int> entries) =>
        (byte[]?)Call(
            compiler,
            "OrderRetainedClosureFrames",
            new ReadOnlyMemory<byte>(bytes),
            5,
            entries
        );

    private static byte[] SoleFrameFixture(bool ordered = false, bool interiorEntry = false)
    {
        int input = ordered ? 1 : 0;
        int result = 1 - input;
        byte[] relocation = ordered ? [Move, Y(0), Y(1)] : [];
        byte[] marker = interiorEntry ? [Label, U(3)] : [];

        return
        [
            .. Start(
                1,
                1,
                1,
                2
            ),
            Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(result), Move, X(0), Y(input),
            Line, U(1), ExternalCall, U(1), U(0), Swap, Y(input), X(0),
            .. marker,
            ExternalCall, U(1), U(1), Move, X(0), Y(result),
            TestHeap, (byte)(U(AllocationList) | ExtendedTag), U(AllocationKinds),
            U(AllocationWords), U(2), U(AllocationFloats), U(0), U(AllocationFuns), U(1), U(0),
            MakeFun, U(0), X(1), ExtendedListOperand, U(2), Y(result), Y(input), .. relocation,
            TrimFrame, U(1), U(1),
            Move, I(5), X(0), Line, U(2), ExternalCall, U(2), U(2),
            Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[] FrameFixture(bool ordered = false, bool interiorEntry = false)
    {
        byte[] saves = ordered
            ? [U(1), Y(0), Move, X(1), Y(1), Move, X(0), Y(2)]
            : [U(2), Y(1), Y(2), Move, X(0), Y(1), Move, X(1), Y(0)];
        byte[] retained = ordered ? [Y(0), Y(2), Move, Y(0), Y(2)] : [Y(2), Y(1), Move, Y(0), Y(1)];
        byte[] finalTrim = ordered ? [TrimFrame, U(1), U(1)] : [];
        byte[] marker = interiorEntry ? [Label, U(3)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(3), U(2), InitYRegisters, ExtendedListOperand, .. saves,
            .. marker,
            TestHeap, (byte)(U(AllocationList) | ExtendedTag), U(AllocationKinds),
            U(AllocationWords), U(2), U(AllocationFloats), U(0), U(AllocationFuns), U(1), U(0),
            MakeFun, U(0), X(1), ExtendedListOperand, U(2), .. retained,
            TrimFrame, U(1), U(2),
            Move, I(5), X(0), Line, U(1), ExternalCall, U(2), U(0),
            Move, Y(0), X(1), Move, Y(1), X(0), .. finalTrim,
            Line, U(2), ExternalCall, U(2), U(1),
            Move, Y(ordered ? 0 : 1), X(0), Deallocate, U(ordered ? 1 : 2), Return, EndCode
        ];
    }

    private static byte[] Fixture(
        bool captureValue = false,
        bool readAfterCall = false,
        bool relocate = true,
        bool readClosure = false
    )
    {
        byte[] use = readAfterCall ? [Move, Y(1), X(0)] : [];
        byte[] relocation = relocate ? [Move, Y(1), Y(3)] : [];
        byte[] finish = relocate
            ? [Move, Y(2), X(0), Deallocate, U(3)]
            : [Move, readClosure ? Y(2) : Y(0), X(0), Move, Y(0), Y(2), TrimFrame, U(2), U(1), Deallocate, U(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(4), U(2),
            InitYRegisters, ExtendedListOperand, U(2), Y(2), Y(3),
            Move, X(0), Y(0), Move, X(1), Y(1),
            Move, ExtendedLiteralOperand, U(0), Y(2),
            TestHeap, (byte)(U(AllocationList) | ExtendedTag), U(AllocationKinds),
            U(AllocationWords), U(1), U(AllocationFloats), U(0), U(AllocationFuns), U(1), U(0),
            MakeFun, U(0), Y(3), ExtendedListOperand, U(1), captureValue ? Y(2) : Y(0),
            Move, Y(3), X(1), .. relocation, TrimFrame, U(1), U(3),
            Move, Y(1), X(0), Line, U(2), ExternalCall, U(2), U(0),
            .. use,
            .. finish, Return, EndCode
        ];
    }
}
