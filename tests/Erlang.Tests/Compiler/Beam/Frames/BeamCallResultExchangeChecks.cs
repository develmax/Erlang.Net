using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCallResultExchangeChecks
{
    internal const string TestName = "compiler/beam/exchanges-call-result-and-saved-input-in-one-frame-cell";
    private const string MissingExchange = "Expected a single saved slot and a swap with the result register.";
    private const string OwnedExchange = "The outgoing exchange must preserve both later value definitions in the original frame.";
    private const string UnusedExchangeSlot = "Expected an owned exchange to release its unused destination slot.";
    private const string UnsafeExchange = "Expected the frame to retain slots across an intervening collector, branch or later read.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Entry = 0x25;
    private const byte Branch = 0x35;
    private const byte Module = 0x12;
    private const byte Function = 0x22;

    internal static Task Run()
    {
        byte[] header = [Label, One, FunctionInfo, Module, Function, Two, Label, Two];
        byte[] tail = [Line, Two, LocalCall, Two, Entry, Deallocate, Two, Return, EndCode];
        byte[] expected =
        [
            .. header, Allocate, One, Two, Move, X1, Y0, LocalCall, One, Entry,
            Swap, Y0, X0, LocalCall, One, Entry, Move, X0, X1, Move, Y0, X0,
            InitYRegisters, ExtendedListOperand, One, Y0,
            Line, Two, LocalCall, Two, Entry, Deallocate, One, Return, EndCode
        ];

        foreach (var (old, saved) in new[] { (Y0, Y1), (Y1, Y0) })
        {
            byte[] frame =
            [
                Allocate, Two, Two, InitYRegisters, ExtendedListOperand, One, saved,
                Move, X1, old, LocalCall, One, Entry
            ];
            byte[] copies = [Move, X0, saved, Move, old, X0];
            byte[] consume = [LocalCall, One, Entry, Move, X0, X1, Move, saved, X0];
            byte[] original = [.. header, .. frame, .. copies, .. consume, .. tail];
            byte[] withDeadSpill =
            [
                .. header, .. frame, .. copies, .. consume,
                Line, Two, LocalCall, Two, Entry, Move, X0, saved, Deallocate, Two, Return, EndCode
            ];
            if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
                || Rewrite(withDeadSpill) is not { } actualSpill || !actualSpill.AsSpan().SequenceEqual(expected))
            {
                throw new InvalidOperationException(MissingExchange);
            }

            byte[] collecting =
            [
                .. header, .. frame, .. copies, TestHeap, One, Two, .. consume, .. tail
            ];
            byte[] merging =
            [
                .. header, .. frame, .. copies, IsAtom, Branch, X0, Label, Three, .. consume, .. tail
            ];
            byte[] laterRead =
            [
                .. header, .. frame, .. copies, .. consume,
                Line, Two, LocalCall, Two, Entry, Move, saved, X0, Deallocate, Two, Return, EndCode
            ];
            if (Rewrite(collecting) is not null || Rewrite(merging) is not null || Rewrite(laterRead) is not null)
            {
                throw new InvalidOperationException(UnsafeExchange);
            }
        }

        CheckOwnedHandoff();
        CheckClosureHandoff();

        return VerifyOwnedExchange();
    }

    private static void CheckClosureHandoff()
    {
        var compiler = new BeamModuleCompiler("owned_closure_exchange_fixture");
        _ = Call(compiler, "Intern", BeamOpcodeEdgeNames.Run);
        byte[] entry = [Label, One, FunctionInfo, Module, Function, Two, Label, Two];
        byte[] suffix = [Line, Two, LocalCall, Two, Entry, TestHeap, Two, One];
        byte[] original =
        [
            .. entry, AllocateHeap, Three, 0, Two, InitYRegisters, ExtendedListOperand, One, Y(2),
            Move, X0, Y0, Move, X1, Y1, LocalCall, One, Entry,
            Move, X0, Y(2), TestHeap, One, 0, MakeFun, 0, X0, ExtendedListOperand, 0,
            Move, Y1, X1, .. suffix, Move, Y0, X1, PutList, Y(2), X0, X0, Deallocate, Three, Return, EndCode
        ];
        byte[] expected =
        [
            .. entry, AllocateHeap, Two, 0, Two,
            Move, X0, Y0, Move, X1, Y1, LocalCall, One, Entry,
            TestHeap, One, One, MakeFun, 0, X1, ExtendedListOperand, 0,
            Move, Y1, X(2), Move, X0, Y1, Move, X1, X0, Move, X(2), X1,
            .. suffix, Move, Y0, X1, PutList, Y1, X0, X0, Deallocate, Two, Return, EndCode
        ];
        byte[] laterUse = original.ToArray();
        int put = Array.IndexOf(laterUse, PutList);
        laterUse[put - 2] = Y1;
        int closure = Array.IndexOf(original, MakeFun);
        byte[] captures = [.. original[..(closure + 4)], One, Y1, .. original[(closure + 5)..]];
        if (Apply(original) is not { } actual || !actual.SequenceEqual(expected)
            || Apply(laterUse) is not null || Apply(captures) is not null)
        {
            throw new InvalidOperationException(UnsafeExchange);
        }

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReuseRetiredClosureInputForCallResult",
            code,
            5
        );
    }

    private static async Task VerifyOwnedExchange()
    {
        var compiler = new BeamModuleCompiler("owned_call_exchange_fixture");
        _ = Call(compiler, "Intern", BeamOpcodeEdgeNames.Run);
        byte[] entry = [Label, One, FunctionInfo, Module, Function, One, Label, Two];
        byte[] prefix =
        [
            Allocate, Two, One, InitYRegisters, ExtendedListOperand, One, Y1,
            Move, X0, Y0, Move, 0x02, X0, ExternalCall, One, 0
        ];
        byte[] moves = [Move, X0, Y1, Move, Y0, X0, InitYRegisters, ExtendedListOperand, One, Y0];
        byte[] call = [ExternalCall, One, 0];
        byte[] finish = [Deallocate, Two, Return];
        byte[] body =
        [
            .. prefix, .. moves, .. call,
            Move, X0, Y0, TestHeap, Three, 0, PutTuple, X0, ExtendedListOperand, Two, Y1, Y0, .. finish
        ];
        byte[] exchanged =
        [
            .. prefix, Swap, Y0, X0, .. call,
            Move, X0, Y1, TestHeap, Three, 0, PutTuple, X0, ExtendedListOperand, Two, Y0, Y1, .. finish
        ];
        if (RewriteOwned(body) is not { } actual || !actual.AsSpan().SequenceEqual([.. entry, .. exchanged, EndCode]))
        {
            throw new InvalidOperationException(OwnedExchange);
        }

        byte[] oneSlotBody = [.. prefix, .. moves, .. call, Move, Y1, X0, .. finish];
        byte[] oneSlotExchange =
        [
            Allocate, One, One, Move, X0, Y0, Move, 0x02, X0, ExternalCall, One, 0,
            Swap, Y0, X0, .. call, Move, Y0, X0, Deallocate, One, Return
        ];
        if (RewriteOwned(oneSlotBody) is not { } compacted
            || !compacted.AsSpan().SequenceEqual([.. entry, .. oneSlotExchange, EndCode]))
        {
            throw new InvalidOperationException(UnusedExchangeSlot);
        }

        byte[] preparedResult = [.. prefix, Move, X0, X1, .. moves, .. call, Move, Y1, X0, .. finish];
        byte[] preparedExchange =
        [
            .. prefix, Move, X0, X1, Swap, Y0, X0, .. call, Move, Y0, X0, .. finish
        ];
        if (RewriteOwned(preparedResult) is not { } prepared
            || !prepared.AsSpan().SequenceEqual([.. entry, .. preparedExchange, EndCode]))
        {
            throw new InvalidOperationException(UnsafeExchange);
        }

        byte[] usedDestination = [.. prefix, Move, X0, Y1, .. body[prefix.Length..]];
        byte[] sharedSuffix =
        [
            .. prefix, IsAtom, Branch, X0, .. moves, .. call, Label, Three, .. body[(prefix.Length + moves.Length + call.Length)..]
        ];
        if (RewriteOwned(usedDestination) is not null || RewriteOwned(sharedSuffix) is not null)
        {
            throw new InvalidOperationException(UnsafeExchange);
        }

        BeamModule transformed = BeamModule.Prepare(BeamOpcodeEdgeBytes.Image([.. entry, .. exchanged]));
        BeamModule original = BeamModule.Prepare(BeamOpcodeEdgeBytes.Image([.. entry, .. body]));
        BeamModule compact = BeamModule.Prepare(BeamOpcodeEdgeBytes.Image(compacted[..^1]));
        BeamModule originalWide = BeamModule.Prepare(BeamOpcodeEdgeBytes.Image([.. entry, .. oneSlotBody]));
        await using var runtime = new ProcessRuntime();
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term input = Term.List(Term.I(5), Term.I(6));
            Term expected = Term.Tuple(Term.I(0), Term.I(2));
            foreach (BeamModule image in new[] { transformed, original })
            {
                if (!expected.Equals(await image.Invoke(context, BeamOpcodeEdgeNames.Run, input)))
                {
                    throw new InvalidOperationException(OwnedExchange);
                }

                try
                {
                    _ = await image.Invoke(context, BeamOpcodeEdgeNames.Run, Term.I(1));

                    throw new InvalidOperationException(UnsafeExchange);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                }
            }

            foreach (BeamModule image in new[] { compact, originalWide })
            {
                if (!Term.I(0).Equals(await image.Invoke(context, BeamOpcodeEdgeNames.Run, input)))
                {
                    throw new InvalidOperationException(UnusedExchangeSlot);
                }
            }

            return Nil.Value;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        if (!exit.Equals(Term.A(ProcessExitReasons.Normal)))
        {
            throw new InvalidOperationException(UnsafeExchange);
        }

        byte[]? RewriteOwned(byte[] instructions)
        {
            byte[] bytes = [.. entry, .. instructions, EndCode];
            ReadOnlyMemory<byte> code = bytes;

            return (byte[]?)Call(
                compiler,
                "ExchangeOwnedCallArgument",
                code,
                5
            );
        }
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
            1,
            instructions
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "ExchangeCallResultWithSavedInput",
            instructions,
            body
        );
    }

    private static void CheckOwnedHandoff()
    {
        var compiler = new BeamModuleCompiler("beam_owned_call_result_handoff");
        foreach (bool argumentInX0 in new[] { true, false })
        {
            if (Apply(Shape(argumentInX0)) is not { } actual
                || !actual.SequenceEqual(Shape(argumentInX0, compacted: true))
                || Apply(actual) is not null || Apply(Shape(argumentInX0, liveInput: true)) is not null
                || Apply(Shape(argumentInX0, bypass: true)) is not null
                || Apply(Shape(argumentInX0, dependentCopy: true)) is not null)
            {
                throw new InvalidOperationException("The retired call input must carry only the new result after the outgoing load.");
            }
        }

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReuseRetiredInputForCallResult",
            code,
            5
        );
    }

    private static byte[] Shape(
        bool argumentInX0,
        bool compacted = false,
        bool liveInput = false,
        bool bypass = false,
        bool dependentCopy = false
    )
    {
        byte old = compacted ? Y(0) : Y(1);
        byte saved = Y(0);
        byte argument = X(argumentInX0 ? 0 : 1);
        byte second = X(argumentInX0 ? 1 : 0);
        byte[] exchange = compacted
            ? (argumentInX0 ? [Swap, old, X(0)] : [Move, old, argument, Move, X(0), saved])
            : [Move, X(0), saved];
        byte[] outgoing = compacted ? [] : [Move, old, argument, InitYRegisters, ExtendedListOperand, U(1), old];
        byte[] initial = compacted ? [] : [InitYRegisters, ExtendedListOperand, U(1), saved];
        byte[] bypassed = bypass ? [IsAtom, F(3), X(0)] : [];
        byte[] keep = liveInput ? [Move, old, X(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(compacted ? 1 : 2), U(1), .. initial, Move, X(0), old, .. bypassed,
            ExternalCall, U(1), U(0), .. exchange, Move, A(1), dependentCopy ? argument : second,
            .. outgoing, Line, U(1), ExternalCall, U(2), U(1), Label, U(3), .. keep,
            Move, saved, X(0), Deallocate, U(compacted ? 1 : 2), Return, EndCode
        ];
    }

}
