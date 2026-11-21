using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAtomicProjectionFrameChecks
{
    internal const string TestName = "compiler/beam/reuses-dead-stack-inputs-in-atomic-projections";
    private const string WrongFrame = "Expected stack aliasing only after the source dies and distinct list outputs to keep their slots.";
    private const string WrongResult = "An aliased projection changed the source failure, a projected term, or a live original input.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Five = 0x50;
    private const byte Six = 0x60;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Handler = 0x45;
    private const byte FunctionClause = 0x15;
    private const byte RecordTag = 0x22;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Failed = 0x35;
    private const byte Join = 0x45;
    private const byte Cold = 0x55;
    private const byte Framed = 0x65;

    internal static async Task Run()
    {
        byte[] tuple = TupleBody(keepInput: false);
        byte[] tupleExpected =
        [
            Allocate, One, One, InitYRegisters, ExtendedListOperand, One, Y0, Move, X0, Y0,
            IsTuple, Failed, Y0, TestArity, Failed, Y0, Two,
            GetTupleElement, Y0, One, Y0, Move, Y0, X0, Deallocate, One, Return,
            Label, Three, BadMatch, Y0
        ];
        RequireRewrite(tuple, tupleExpected);

        byte[] nestedPrefix =
        [
            IsTaggedTuple, FunctionClause, X0, Two, RecordTag,
            GetTupleElement, X0, One, X1,
            IsTaggedTuple, FunctionClause, X1, Two, RecordTag
        ];
        byte[] nestedTuple = [.. nestedPrefix, .. tuple];
        byte[] nestedExpected = [.. nestedPrefix, .. tupleExpected];
        if (Rewrite(nestedTuple, nestedCheckedEntriesOnly: true) is not { } nested
            || !nested.AsSpan().SequenceEqual(nestedExpected)
            || Rewrite(
                [.. nestedPrefix, .. TupleBody(keepInput: true)],
                nestedCheckedEntriesOnly: true
            ) is not null
            || Rewrite(tuple, nestedCheckedEntriesOnly: true) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        byte[] arityPrefix =
        [
            IsTaggedTuple, FunctionClause, X0, Two, RecordTag, GetTupleElement, X0, One, X1,
            IsTuple, FunctionClause, X1, TestArity, FunctionClause, X1, Two
        ];
        byte[] splitCarrier =
        [
            .. arityPrefix, Allocate, Three, Two, Move, X0, Y2, Move, X1, Y0,
            GetTupleElement, Y0, One, Y1, IsInteger, Failed, Y1, Jump, Join,
            Label, Three, BadMatch, Y1, Label, Four, Move, Y2, X0, ExternalCall, One, 0,
            Move, X0, Y0, Move, Y1, X1, Move, Y0, X0, ExternalCall, Two, One, Deallocate, Three, Return
        ];
        byte[] liveSpare =
        [
            .. splitCarrier[..^6], Move, Y2, 0x23, ExternalCall, Three, One, Deallocate, Three, Return
        ];
        if (Rewrite(splitCarrier, nestedCheckedEntriesOnly: true) is not { } split
            || split[arityPrefix.Length + 1] != Two
            || Rewrite(liveSpare, nestedCheckedEntriesOnly: true) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        byte[] coldPrefix =
        [
            IsNonemptyList, Cold, X0, Jump, Framed,
            Label, Five, Line, One, BadMatch, X0,
            Label, Six
        ];
        byte[]? ownedList = null;
        foreach (bool headAlias in new[] { true, false })
        {
            byte head = headAlias ? Y1 : Y0;
            byte tail = headAlias ? Y0 : Y1;
            byte[] listExpected =
            [
                Allocate, Two, One, InitYRegisters, ExtendedListOperand, Two, Y0, Y1, Move, X0, Y1,
                IsNonemptyList, Failed, Y1, GetList, Y1, head, tail,
                IsAtom, Join, head, Jump, Join, Label, Four,
                TestHeap, Three, 0, PutTuple, X0, ExtendedListOperand, Two, head, tail, Deallocate, Two, Return,
                Label, Three, BadMatch, Y1
            ];
            byte[] original = ListBody(headAlias, keepInput: false);
            RequireRewrite(original, listExpected);
            RequireRewrite([.. coldPrefix, .. original], [.. coldPrefix, .. listExpected]);
            ownedList = [.. coldPrefix, .. listExpected];
        }

        byte[] tupleLive = TupleBody(keepInput: true);
        byte[] listLive = ListBody(headAlias: true, keepInput: true);
        if (Rewrite(tupleLive) is not null || Rewrite(listLive) is not null
            || Rewrite(tuple, atomicOnly: false) is not null
            || Rewrite([.. coldPrefix, .. listLive]) is not null
            || Rewrite([.. coldPrefix, .. ListBody(headAlias: true, keepInput: false)], atomicOnly: false) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        byte[] closedTryProjection = TryProjectionBody(compacted: true);
        RequireRewrite(TryProjectionBody(), closedTryProjection);
        if (Rewrite(TryProjectionBody(active: true)) is not null
            || Rewrite(TryProjectionBody(keepInput: true)) is not null
            || Rewrite(TryProjectionBody(reuseTag: true)) is not null)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        byte[] recordCall =
        [
            IsTaggedTuple, FunctionClause, X0, Two, RecordTag,
            GetTupleElement, X0, One, X1,
            IsTaggedTuple, FunctionClause, X1, Two, RecordTag,
            GetTupleElement, X1, One, 0x23, GetTupleElement, X1, 0, 0x33,
            Allocate, Three, Four, Move, X1, Y0, Move, 0x23, Y1, Move, X0, Y2,
            GetTupleElement, Y0, One, 0x23, Move, 0x33, X1, Move, Y1, X0, ExternalCall, Three, 0
        ];
        byte[] recordUpdates =
        [
            IsTaggedTuple, Failed, Y0, Two, RecordTag, TestHeap, Six, 0,
            UpdateRecord, RecordTag, Two, Y0, X0, ExtendedListOperand, Two, One, 0x02, Move, X0, Y1,
            UpdateRecord, RecordTag, Two, Y2, X0, ExtendedListOperand, Two, One, Y1,
            PutTuple, X0, ExtendedListOperand, Two, X0, 0x02,
            Deallocate, Three, Return, Label, Three, BadRecord, Y0
        ];
        if (!HasRecordCallReuse([.. recordCall, .. recordUpdates])
            || HasRecordCallReuse([.. recordCall, Move, Y1, X1, .. recordUpdates]))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        BeamModule tupleImage = BeamModule.Prepare(BeamBinaryHostBytes.Image(tupleExpected));
        BeamModule tupleLiveImage = BeamModule.Prepare(BeamBinaryHostBytes.Image(tupleLive));
        BeamModule listHead = BeamModule.Prepare(BeamBinaryHostBytes.Image(Rewrite(ListBody(headAlias: true, keepInput: false))!, labels: 5));
        BeamModule listTail = BeamModule.Prepare(BeamBinaryHostBytes.Image(Rewrite(ListBody(headAlias: false, keepInput: false))!, labels: 5));
        BeamModule listLiveImage = BeamModule.Prepare(BeamBinaryHostBytes.Image(listLive, labels: 5));
        BeamModule ownedImage = BeamModule.Prepare(BeamBinaryHostBytes.Image(ownedList!, labels: 7));
        BeamModule tryProjectionImage = BeamModule.Prepare(BeamBinaryHostBytes.Image(closedTryProjection, labels: 5));
        await using var runtime = new ProcessRuntime();
        Term a = Term.A("a");
        Term b = Term.A("b");
        Term pair = Term.Tuple(b, a);
        Term list = Term.List(a, b);
        Term projected = Term.Tuple(a, Term.List(b));
        Term payload = Term.A("not_a_constructed_term");
        ProcessHandle process = runtime.Spawn(async context =>
        {
            if (!a.Equals(await tupleImage.Invoke(context, "run", pair))
                || !Term.Tuple(pair, a).Equals(await tupleLiveImage.Invoke(context, "run", pair))
                || !projected.Equals(await listHead.Invoke(context, "run", list))
                || !projected.Equals(await listTail.Invoke(context, "run", list))
                || !projected.Equals(await ownedImage.Invoke(context, "run", list))
                || !a.Equals(await tryProjectionImage.Invoke(context, "run", pair))
                || !Term.Tuple(Term.A("badmatch"), payload).Equals(await tryProjectionImage.Invoke(context, "run", payload))
                || !Term.Tuple(list, a, Term.List(b)).Equals(await listLiveImage.Invoke(context, "run", list)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            foreach (BeamModule image in new[] { tupleImage, listHead, listTail, ownedImage })
            {
                try
                {
                    _ = await image.Invoke(context, "run", payload);

                    throw new InvalidOperationException(WrongResult);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), payload)))
                {
                }
            }

            return Term.A("ok");
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        if (!exit.Equals(Term.A("normal")))
        {
            throw new InvalidOperationException(WrongResult);
        }
    }

    private static byte[] TupleBody(bool keepInput)
    {
        byte[] output = keepInput
            ? [TestHeap, Three, 0, PutTuple, X0, ExtendedListOperand, Two, Y0, Y1]
            : [Move, Y1, X0];

        return
        [
            Allocate, Two, One, InitYRegisters, ExtendedListOperand, One, Y1, Move, X0, Y0,
            IsTuple, Failed, Y0, TestArity, Failed, Y0, Two,
            GetTupleElement, Y0, One, Y1, .. output, Deallocate, Two, Return, Label, Three, BadMatch, Y0
        ];
    }

    private static byte[] TryProjectionBody(
        bool compacted = false,
        bool active = false,
        bool keepInput = false,
        bool reuseTag = false
    )
    {
        byte frame = compacted || reuseTag ? Two : Three;
        byte tag = compacted ? Y1 : reuseTag ? Y0 : Y2;
        byte result = compacted ? Y0 : Y1;
        byte[] close = [TryEnd, tag];
        byte[] carryInput = reuseTag
            ? [TryEnter, tag, Handler]
            : [Move, X0, result, TryEnter, tag, Handler, Move, result, X0];
        byte[] setup = reuseTag
            ? [.. close, Move, X0, Y0]
            : [Move, X0, Y0];
        byte[] output = keepInput
            ? [TestHeap, Three, 0, PutTuple, X0, ExtendedListOperand, Two, Y0, result]
            : [Move, result, X0];

        return
        [
            Allocate, frame, One,
            InitYRegisters, ExtendedListOperand, One, reuseTag ? Y1 : Y0,
            .. carryInput, .. setup,
            IsTuple, Failed, Y0, TestArity, Failed, Y0, Two,
            .. (active || reuseTag ? [] : close), GetTupleElement, Y0, One, result,
            .. (active ? close : []), .. output, Deallocate, frame, Return,
            Label, Three, BadMatch, Y0,
            Label, Four, TryCase, tag, Move, X1, X0, Deallocate, frame, Return
        ];
    }

    private static byte[] ListBody(bool headAlias, bool keepInput)
    {
        byte head = headAlias ? Y1 : Y2;
        byte tail = headAlias ? Y2 : Y1;
        byte[] output = keepInput ? [Y0, head, tail] : [head, tail];

        return
        [
            Allocate, Three, One, InitYRegisters, ExtendedListOperand, Two, Y1, Y2, Move, X0, Y0,
            IsNonemptyList, Failed, Y0, GetList, Y0, head, tail,
            IsAtom, Join, head, Jump, Join, Label, Four,
            TestHeap, keepInput ? Four : Three, 0, PutTuple, X0, ExtendedListOperand, keepInput ? Three : Two, .. output,
            Deallocate, Three, Return, Label, Three, BadMatch, Y0
        ];
    }

    private static bool HasRecordCallReuse(byte[] body)
    {
        byte[] entry = [Label, One, FunctionInfo, 0x12, 0x22, One, Label, Two];
        byte[] encoded = [.. entry, .. body, EndCode];
        object header = New(
            "BeamCodeHeader",
            0,
            (int)UpdateRecord,
            7,
            1,
            new ReadOnlyMemory<byte>(encoded)
        );
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return Call(
            typeof(BeamModuleCompiler),
            "PlanNestedRecordCallOperandReuse",
            instructions,
            2,
            (int)Property(instructions, "Count") - 1
        ) is not null;
    }

    private static void RequireRewrite(byte[] body, byte[] expected)
    {
        if (Rewrite(body) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(WrongFrame);
        }
    }

    private static byte[]? Rewrite(
        byte[] body,
        bool atomicOnly = true,
        bool nestedCheckedEntriesOnly = false
    )
    {
        byte[] entry = [Label, One, FunctionInfo, 0x12, 0x22, One, Label, Two];
        byte[] encoded = [.. entry, .. body, EndCode];
        ReadOnlyMemory<byte> code = encoded;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            7,
            1,
            code
        );
        var stackOperands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> stack = (start, end, slot) => stackOperands.Add((start, end, slot));
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            stack
        );
        byte[]? rewritten = (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteReusedStackSlots",
            code,
            instructions,
            stackOperands,
            false,
            null,
            atomicOnly,
            nestedCheckedEntriesOnly
        );

        return rewritten is null ? null : rewritten[entry.Length..^1];
    }
}
