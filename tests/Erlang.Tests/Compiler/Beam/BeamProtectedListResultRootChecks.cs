using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProtectedListResultRootChecks
{
    internal const string TestName = "compiler/beam/keeps-protected-list-construction-result-in-x0";
    private const string HeadImport = "hd";
    private const string Failure = "Protected list construction changed its retained result, fallback, or root prefix.";
    private const int Handler = 3;
    private const int ProducerInfo = 4;
    private const int Producer = 5;
    private const int ProducerFailure = 6;
    private const int LabelCount = ProducerFailure + 1;
    private const int RetiredSlot = 0;
    private const int ExitLabelCount = 10;
    private const string ClosedExitPass = "TrimClosedCatchPayloadExits";
    private const string SuccessFieldsPass = "ScheduleClosedCatchSuccessFields";
    private const int SuccessLabels = 12;

    internal static async Task Run()
    {
        byte[] before = Fixture();
        if (Rewrite(before) is not { } after || !after.SequenceEqual(Fixture(compact: true))
            || Rewrite(after) is not null || Rewrite(Fixture(usesRetiredOnFailure: true)) is not null
            || Rewrite(Fixture(oldHeapLive: 1)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        CheckTypedClosedCatchResult();
        byte[] closed = ClosedCatchFixture();
        int width = Start(
            1,
            2,
            2,
            3
        ).Length + 1;
        const string closedAliasPass = "KeepClosedCatchNilResultInX0";
        const int closedLabels = 10;
        if (Rewrite(closed, closedAliasPass, closedLabels) is not { } closedAfter
            || closed[width] != U(5) || closedAfter[width] != U(4)
            || Rewrite(closedAfter, closedAliasPass, closedLabels) is not null
            || Rewrite(ClosedCatchFixture(oldHeapLive: 1), closedAliasPass, closedLabels) is not null
            || Rewrite(ClosedCatchFixture(useResultOnNilArm: true), closedAliasPass, closedLabels) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        byte[] exitBefore = ClosedExitFixture();
        if (RewriteExit(exitBefore) is not { } exitAfter
            || !exitAfter.SequenceEqual(ClosedExitFixture(trimmed: true))
            || RewriteExit(exitAfter) is not null
            || RewriteExit(ClosedExitFixture(secondRoot: true)) is not null
            || RewriteExit(exitBefore, import: HeadImport) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        byte[] successBefore = SuccessFieldsFixture();
        if (Rewrite(successBefore, SuccessFieldsPass, SuccessLabels) is not { } successAfter
            || !successAfter.SequenceEqual(SuccessFieldsFixture(rewritten: true))
            || Rewrite(successAfter, SuccessFieldsPass, SuccessLabels) is not null
            || Rewrite(SuccessFieldsFixture(secondNilRead: true), SuccessFieldsPass, SuccessLabels) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        var original = Prepare(before);
        var changed = Prepare(after);
        var closedOriginal = PrepareClosed(closed);
        var closedChanged = PrepareClosed(closedAfter);
        var exitOriginal = PrepareExit(exitBefore);
        var exitChanged = PrepareExit(exitAfter);
        var successOriginal = PrepareSuccess(successBefore);
        var successChanged = PrepareSuccess(successAfter);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.A(BeamOpcodeEdgeNames.Marker);
            Term fallback = Term.A(BeamOpcodeEdgeNames.Other);
            foreach (BeamModule module in new[] { original, changed })
            {
                if (!value.Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    value,
                    fallback
                )) || !fallback.Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    Term.I(42),
                    fallback
                )))
                {
                    throw new InvalidOperationException(Failure);
                }
            }

            Term end = Term.A("updated");
            Term old = Term.A("old");
            Term tokens = Term.List(value);
            Term scan = Term.Tuple(tokens, end);
            foreach (BeamModule module in new[] { closedOriginal, closedChanged })
            {
                if (!Term.Tuple(
                    scan,
                    fallback,
                    tokens,
                    end
                ).Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    scan,
                    fallback,
                    old
                )) || !end.Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    Term.Tuple(Nil.Value, end),
                    fallback,
                    old
                )) || !old.Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    value,
                    fallback,
                    old
                )))
                {
                    throw new InvalidOperationException(Failure);
                }
            }

            Term successful = Term.Tuple(value, fallback, old);
            foreach (BeamModule module in new[] { exitOriginal, exitChanged })
            {
                if (!successful.Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    successful,
                    old
                )))
                {
                    throw new InvalidOperationException(Failure);
                }

                try
                {
                    await module.Invoke(
                        context,
                        BeamOpcodeEdgeNames.Run,
                        Term.Tuple(value, fallback),
                        old
                    );
                    throw new InvalidOperationException(Failure);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit
                    && error.Reason.Equals(fallback))
                {
                    // The exit arm still delivers the original caught payload.
                }
            }

            Term otherSuccess = Term.Tuple(fallback, value, old);
            foreach (BeamModule module in new[] { successOriginal, successChanged })
            {
                if (!Term.Tuple(
                    value,
                    tokens,
                    tokens,
                    end
                ).Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    Term.Tuple(value, tokens, end),
                    fallback,
                    old
                )) || !end.Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    Term.Tuple(value, Nil.Value, end),
                    fallback,
                    old
                )) || !otherSuccess.Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    otherSuccess,
                    fallback,
                    old
                )))
                {
                    throw new InvalidOperationException(Failure);
                }
            }

            return Nil.Value;
        });
        await CheckClosedCatchTupleGroups(runtime);
    }

    private static async Task CheckClosedCatchTupleGroups(ProcessRuntime runtime)
    {
        const int labels = 13;
        const string hoist = "HoistClosedCatchTupleFields";
        byte[] before = TupleGroupsFixture();
        if (Rewrite(before, hoist, labels) is not { } after
            || !after.SequenceEqual(TupleGroupsFixture(hoistFirst: true, hoistSecond: true))
            || CollectTupleGroupRelays(before, labels) != 1
            || Rewrite(TupleGroupsFixture(readSiblingSlot: true), hoist, labels) is not { } restricted
            || !restricted.SequenceEqual(TupleGroupsFixture(hoistSecond: true, readSiblingSlot: true))
            || CollectTupleGroupRelays(TupleGroupsFixture(readSiblingSlot: true), labels) != 0)
        {
            throw new InvalidOperationException(Failure);
        }

        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term match = Term.A(BeamOpcodeEdgeNames.Marker);
            Term identity = Term.A(BeamOpcodeEdgeNames.Other);
            Term field = Term.A("retained");
            Term pair = Term.Tuple(identity, field);
            Term triple = Term.Tuple(identity, Nil.Value, field);
            foreach (byte[] code in new[] { before, after })
            {
                var module = BeamModule.Prepare(Image(
                    code[..^1],
                    labels: labels,
                    functions: 2,
                    importFunction: HeadImport
                ));
                if (!field.Equals(await module.Invoke(context, BeamOpcodeEdgeNames.Run, Term.Tuple(match, field)))
                    || !field.Equals(await module.Invoke(context, BeamOpcodeEdgeNames.Run, Term.Tuple(match, Nil.Value, field)))
                    || !pair.Equals(await module.Invoke(context, BeamOpcodeEdgeNames.Run, pair))
                    || !triple.Equals(await module.Invoke(context, BeamOpcodeEdgeNames.Run, triple)))
                {
                    throw new InvalidOperationException(Failure);
                }

                try
                {
                    await module.Invoke(context, BeamOpcodeEdgeNames.Run, field);
                    throw new InvalidOperationException(Failure);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), field)))
                {
                    // The original rejected term is not replaced by either projection.
                }
            }

            return Nil.Value;
        });
    }

    private static int CollectTupleGroupRelays(byte[] instructions, int labels)
    {
        var references = new List<(int Start, int End, int Label)>();
        Action<int, int, int> collect = (start, end, label) => references.Add((start, end, label));
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            labels,
            2,
            (ReadOnlyMemory<byte>)instructions
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            9,
            0,
            collect,
            null
        );
        var relays = new Dictionary<int, (int Target, int End)>();
        _ = Call(
            typeof(BeamModuleCompiler),
            "CollectClosedCatchArityReturnRelays",
            body,
            references,
            relays
        );

        return relays.Count;
    }

    private static byte[] TupleGroupsFixture(
        bool hoistFirst = false,
        bool hoistSecond = false,
        bool readSiblingSlot = false
    )
    {
        const int frame = 3;
        const int landing = 3;
        const int triple = 4;
        const int tripleKeep = 5;
        const int tripleIdentity = 6;
        const int pair = 7;
        const int pairKeep = 8;
        const int pairIdentity = 9;
        const int rejected = 10;
        const int readerInfo = 11;
        const int reader = 12;
        byte[] tripleRead = [GetTupleElement, X(0), U(2), Y(2)];
        byte[] pairRead = [GetTupleElement, X(0), U(1), Y(2)];
        byte[] extraRead = readSiblingSlot ? [Move, Y(2), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(frame), U(1), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            CatchEnter, Y(2), F(landing), LocalCall, U(1), F(reader),
            Label, U(landing), CatchEnd, Y(2),
            IsTuple, F(rejected), X(0),
            SelectTupleArity, X(0), F(rejected), ExtendedListOperand, U(4), U(2), F(pair), U(3), F(triple),
            Label, U(triple), GetTupleElement, X(0), U(0), X(1), .. (hoistFirst ? tripleRead : []),
            SelectValue, X(1), F(rejected), ExtendedListOperand, U(4), A(7), F(tripleKeep), A(8), F(tripleIdentity),
            Label, U(tripleKeep), .. (hoistFirst ? [] : tripleRead), Move, Y(2), X(0), Deallocate, U(frame), Return,
            Label, U(tripleIdentity), .. extraRead, Deallocate, U(frame), Return,
            Label, U(pair), GetTupleElement, X(0), U(0), X(1), .. (hoistSecond ? pairRead : []),
            SelectValue, X(1), F(rejected), ExtendedListOperand, U(4), A(7), F(pairKeep), A(8), F(pairIdentity),
            Label, U(pairKeep), .. (hoistSecond ? [] : pairRead), Move, Y(2), X(0), Deallocate, U(frame), Return,
            Label, U(pairIdentity), Deallocate, U(frame), Return,
            Label, U(rejected), CaseEnd, X(0),
            .. Start(
                readerInfo,
                reader,
                3,
                1
            ),
            Return, EndCode
        ];
    }

    private static BeamModule Prepare(byte[] code) => BeamModule.Prepare(Image(
        code[..^1],
        labels: LabelCount,
        functions: 2,
        arity: 2,
        importFunction: HeadImport
    ));

    private static BeamModule PrepareClosed(byte[] code) => BeamModule.Prepare(Image(
        code[..^1],
        labels: 10,
        functions: 3,
        arity: 3,
        importFunction: HeadImport
    ));

    private static BeamModule PrepareSuccess(byte[] code) => BeamModule.Prepare(Image(
        code[..^1],
        labels: SuccessLabels,
        functions: 3,
        arity: 3,
        importFunction: HeadImport
    ));

    private static BeamModule PrepareExit(byte[] code) => BeamModule.Prepare(Image(
        code[..^1],
        labels: ExitLabelCount,
        functions: 2,
        arity: 2,
        importFunction: AutoImportNames.Exit
    ));

    private static byte[]? RewriteExit(byte[] code, string import = AutoImportNames.Exit) =>
        Rewrite(
            code,
            ClosedExitPass,
            ExitLabelCount,
            import
        );

    private static byte[]? Rewrite(
        ReadOnlyMemory<byte> code,
        string method = "KeepProtectedListResultInX0",
        int labelCount = LabelCount,
        string import = HeadImport,
        Dictionary<int, int>? resultTypes = null
    )
    {
        var compiler = new BeamModuleCompiler(BeamOpcodeEdgeNames.Module);
        string[] atoms =
        [
            BeamOpcodeEdgeNames.Run,
            BeamOpcodeEdgeNames.Middle,
            BeamOpcodeEdgeNames.Leaf,
            BeamOpcodeEdgeNames.Erlang,
            import,
            BeamOpcodeEdgeNames.Marker,
            BeamOpcodeEdgeNames.Other
        ];
        foreach (string atom in atoms)
        {
            _ = Call(compiler, "Intern", atom);
        }

        _ = Call(
            compiler,
            "Import",
            BeamOpcodeEdgeNames.Erlang,
            import,
            1
        );

        return resultTypes is null ? (byte[]?)Call(
            compiler,
            method,
            code,
            labelCount
        ) : (byte[]?)Call(
            compiler,
            method,
            code,
            labelCount,
            resultTypes
        );
    }

    private static byte[] SuccessFieldsFixture(bool rewritten = false, bool secondNilRead = false)
    {
        const int width = 4;
        const int landing = 3;
        const int group = 4;
        const int selected = 5;
        const int retained = 6;
        const int other = 7;
        const int readerInfo = 8;
        const int reader = 9;
        const int retryInfo = 10;
        const int retry = 11;
        int result = rewritten ? 0 : 3;
        int location = rewritten ? 3 : 0;
        byte[] projections = rewritten
            ? [GetTupleElement, X(0), U(2), Y(3), GetTupleElement, X(0), U(1), X(0),
                InitYRegisters, ExtendedListOperand, U(1), Y(0)]
            : [GetTupleElement, X(0), U(1), Y(3), GetTupleElement, X(0), U(2), Y(0),
                Move, Y(3), X(0)];

        return
        [
            .. Start(
                1,
                2,
                2,
                3
            ),
            Allocate, U(width), U(3), Move, X(2), Y(0), Move, X(1), Y(1), Move, X(0), Y(2),
            CatchEnter, Y(3), F(landing), LocalCall, U(1), F(reader),
            Label, U(landing), CatchEnd, Y(3),
            IsTuple, F(other), X(0),
            SelectTupleArity, X(0), F(other), ExtendedListOperand, U(4), U(2), F(other), U(3), F(group),
            Label, U(group), GetTupleElement, X(0), U(0), X(1),
            SelectValue, X(1), F(other), ExtendedListOperand, U(4), A(7), F(selected), A(8), F(other),
            Label, U(selected), .. projections, LocalCall, U(1), F(reader),
            Move, X(0), Y(result), LocalCall, U(1), F(reader), IsNil, F(retained), X(0),
            Move, Y(secondNilRead ? result : 1), X(1), Move, Y(location), X(2), Move, Y(2), X(0),
            TailLocalCallWithFrame, U(3), F(retry), U(width),
            Label, U(retained), TestHeap, U(5), U(1),
            PutTuple, X(0), ExtendedListOperand, U(4), A(7), Y(result), X(0), Y(location),
            Deallocate, U(width), Return,
            Label, U(other), Deallocate, U(width), Return,
            .. Start(
                readerInfo,
                reader,
                3,
                1
            ),
            Return,
            .. Start(
                retryInfo,
                retry,
                4,
                3
            ),
            Move, X(2), X(0), Return, EndCode
        ];
    }

    private static byte[] ClosedExitFixture(bool trimmed = false, bool secondRoot = false)
    {
        const int frame = 4;
        const int landing = 3;
        const int pair = 4;
        const int triple = 5;
        const int exit = 6;
        const int reject = 7;
        const int readerInfo = 8;
        const int reader = 9;
        byte[] trim = trimmed ? [TrimFrame, U(frame - 1), U(1)] : [];
        byte[] extra = secondRoot ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            Allocate, U(frame), U(2), InitYRegisters, ExtendedListOperand, U(2), Y(1), Y(2),
            Move, X(1), Y(0), CatchEnter, Y(3), F(landing),
            LocalCall, U(1), F(reader),
            Label, U(landing), CatchEnd, Y(3),
            IsTuple, F(reject), X(0),
            SelectTupleArity, X(0), F(reject), ExtendedListOperand, U(4), U(2), F(pair), U(3), F(triple),
            Label, U(pair), GetTupleElement, X(0), U(0), X(1),
            SelectValue, X(1), F(reject), ExtendedListOperand, U(4), A(7), F(exit), A(8), F(triple),
            Label, U(triple), Deallocate, U(frame), Return,
            Label, U(exit), GetTupleElement, X(0), U(1), Y(3), Move, Y(0), X(0), .. trim,
            LocalCall, U(1), F(reader), TestHeap, U(ConsHeapWords), U(1),
            PutList, X(0), A(0), X(0), .. extra, LocalCall, U(1), F(reader),
            Move, Y(trimmed ? 0 : 3), X(0), TailExternalCallWithFrame, U(1), U(0), U(trimmed ? 1 : frame),
            Label, U(reject), CaseEnd, X(0),
            .. Start(
                readerInfo,
                reader,
                3,
                1
            ),
            Return, EndCode
        ];
    }

    private static void CheckTypedClosedCatchResult()
    {
        const int labelCount = 12;
        const int listLabel = 11;
        const int listBits = 0x0084;
        const string rewrite = "KeepClosedCatchNilResultInX0";
        byte[] closed = ClosedCatchFixture(provenListProducer: true);
        byte[]? plain = Rewrite(
            closed,
            rewrite,
            labelCount
        );
        byte[]? wrongLabel = Rewrite(
            closed,
            rewrite,
            labelCount,
            resultTypes: new Dictionary<int, int> { [listLabel - 1] = listBits }
        );
        byte[]? typed = Rewrite(
            closed,
            rewrite,
            labelCount,
            resultTypes: new Dictionary<int, int> { [listLabel] = listBits }
        );
        if (plain is null || wrongLabel is null || typed is null || !plain.SequenceEqual(wrongLabel))
        {
            throw new InvalidOperationException(Failure);
        }

        int nil = Enumerable.Range(0, plain.Length - 2).Single(index =>
            plain[index] == IsNil && plain[index + 1] == F(8) && plain[index + 2] == X(0));
        if (!typed.SequenceEqual(
            plain[..(nil + 2)].Concat(new byte[] { ExtendedTypedRegisterOperand, X(0), U(1) })
                .Concat(plain[(nil + 3)..])))
        {
            throw new InvalidOperationException(Failure);
        }
    }

    private static byte[] ClosedCatchFixture(
        int oldHeapLive = 0,
        bool useResultOnNilArm = false,
        bool provenListProducer = false
    )
    {
        const int width = 5;
        const int landing = 3;
        const int readInfo = 4;
        const int readEntry = 5;
        const int fallbackInfo = 6;
        const int fallbackEntry = 7;
        const int keep = 8;
        const int reject = 9;
        const int listInfo = 10;
        const int listEntry = 11;
        byte[] listProducer = provenListProducer
            ? [.. Start(
                listInfo,
                listEntry,
                7,
                1
            ), Move, A(0), X(0), Return]
            : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                3
            ),
            Allocate, U(width), U(3), InitYRegisters, ExtendedListOperand, U(1), Y(4),
            Move, X(0), Y(0), Move, X(1), Y(1), Move, X(2), Y(2), CatchEnter, Y(3), F(landing),
            LocalCall, U(1), F(readEntry),
            Label, U(landing), CatchEnd, Y(3),
            IsTuple, F(reject), X(0), TestArity, F(reject), X(0), U(2),
            GetTupleElement, X(0), U(1), Y(4), GetTupleElement, X(0), U(0), X(0),
            LocalCall, U(1), F(provenListProducer ? listEntry : readEntry),
            Move, X(0), Y(2), IsNil, F(keep), Y(2),
            Move, Y(useResultOnNilArm ? 2 : 1), X(1), Move, Y(4), X(2), Move, Y(0), X(0),
            TailLocalCallWithFrame, U(3), F(fallbackEntry), U(width),
            Label, U(keep), TestHeap, U(5), U(oldHeapLive),
            PutTuple, X(0), ExtendedListOperand, U(4), Y(0), Y(1), Y(2), Y(4),
            Deallocate, U(width), Return,
            Label, U(reject), Move, Y(2), X(0), Deallocate, U(width), Return,
            .. Start(
                readInfo,
                readEntry,
                3,
                1
            ), Return,
            .. Start(
                fallbackInfo,
                fallbackEntry,
                4,
                3
            ), Move, X(2), X(0), Return,
            .. listProducer, EndCode
        ];
    }

    private static byte[] Fixture(bool compact = false, bool usesRetiredOnFailure = false, int oldHeapLive = 0)
    {
        int frame = compact ? 2 : 3;
        int saved = compact ? 0 : 1;
        int handlerSlot = compact ? 1 : 2;
        int accumulator = compact ? 1 : 0;
        byte[] initialize = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(RetiredSlot)];
        byte[] save = compact ? [] : [Move, X(0), Y(RetiredSlot)];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            Allocate, U(frame), U(2), .. initialize,
            Move, X(1), Y(saved), TryEnter, Y(handlerSlot), F(Handler),
            LocalCall, U(1), F(Producer), .. save,
            TestHeap, U(6), U(compact ? 1 : oldHeapLive),
            PutList, Y(saved), A(0), X(accumulator), PutList, A(7), X(accumulator), X(accumulator),
            PutList, compact ? X(0) : Y(RetiredSlot), X(accumulator), X(0),
            ExternalCall, U(1), U(0), TryEnd, Y(handlerSlot), Deallocate, U(frame), Return,
            Label, U(Handler), TryCase, Y(handlerSlot),
            Move, Y(usesRetiredOnFailure ? RetiredSlot : saved), X(0), Deallocate, U(frame), Return,
            .. Start(
                ProducerInfo,
                Producer,
                3,
                1
            ),
            IsAtom, F(ProducerFailure), X(0), Return,
            Label, U(ProducerFailure), CaseEnd, X(0), EndCode
        ];
    }
}
