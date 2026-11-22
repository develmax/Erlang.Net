using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedConsRetryGateChecks
{
    internal const string TestName = "compiler/beam/bypasses-rejected-state-before-checked-cons-retry";
    private const string WrongPath = "A retry may reuse the positive cons fact only along its owning edges.";
    private const int FunctionInfoLabel = 1;
    private const int EntryLabel = 2;
    private const int RetryLabel = 3;
    private const int RejectLabel = 4;
    private const int LabelCount = 5;
    private const int Arity = 2;

    internal static async Task Run()
    {
        CheckRepeatedTaggedPrefix();
        CheckDistinctTaggedPrefixes();

        byte[] savedOriginal = Fixture(savedState: true);
        if (Rewrite(savedOriginal) is not { } savedRewritten
            || !savedRewritten.SequenceEqual(Fixture(shared: true, savedState: true))
            || Rewrite(savedRewritten) is not null
            || Rewrite(Fixture(otherPredecessor: true, savedState: true)) is not null
            || Rewrite(Fixture(overwriteState: true, savedState: true)) is not null)
        {
            throw new InvalidOperationException(WrongPath);
        }

        byte[] original = Fixture();
        if (Rewrite(original) is not { } rewritten || !rewritten.SequenceEqual(Fixture(shared: true))
            || Rewrite(Fixture(otherPredecessor: true)) is not null
            || Rewrite(Fixture(overwriteState: true)) is not null
            || Rewrite(Fixture(observeSkippedRead: true)) is not null)
        {
            throw new InvalidOperationException(WrongPath);
        }

        var before = BeamModule.Prepare(Image(original[..^1], labels: LabelCount, arity: Arity));
        var after = BeamModule.Prepare(Image(rewritten[..^1], labels: LabelCount, arity: Arity));
        var savedBefore = BeamModule.Prepare(Image(savedOriginal[..^1], labels: LabelCount, arity: Arity));
        var savedAfter = BeamModule.Prepare(Image(savedRewritten[..^1], labels: LabelCount, arity: Arity));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term close = Term.Tuple(Term.I(3), Term.I(7));
            Term other = Term.Tuple(Term.I(3), Term.I(8));
            Term invalid = Term.A("invalid");
            Term head = Term.I(9);
            Term tail = Term.I(4);
            Term singleton = Term.List(head);
            Term pair = Term.List(head, tail);
            var cases = new (Term Input, Term State, Term Expected)[]
            {
                (close, pair, Term.List(tail)),
                (close, singleton, head),
                (other, singleton, singleton),
                (close, Nil.Value, Nil.Value),
                (close, invalid, invalid),
                (invalid, singleton, singleton)
            };
            foreach (var (input, state, expected) in cases)
            {
                Term prior = await before.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    state
                );
                Term current = await after.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    state
                );
                Term stackPrior = await savedBefore.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    state
                );
                Term stackCurrent = await savedAfter.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    state
                );
                if (!expected.Equals(prior) || !expected.Equals(current)
                    || !expected.Equals(stackPrior) || !expected.Equals(stackCurrent))
                {
                    throw new InvalidOperationException(WrongPath);
                }
            }

            return Nil.Value;
        });
    }

    private static void CheckDistinctTaggedPrefixes()
    {
        var compiler = new BeamModuleCompiler("distinct_prefix_fixture");
        int function = (int)Call(compiler, "Intern", "match");
        int first = (int)Call(compiler, "Intern", "first");
        int second = (int)Call(compiler, "Intern", "second");
        byte[] entry = Start(
            FunctionInfoLabel,
            EntryLabel,
            function,
            4
        );
        byte[] firstTail = [IsNil, F(RetryLabel), X(3), Move, X(3), X(0), Return];
        byte[] secondTail = [Move, X(3), X(0), Return];
        byte[] fallback = [Label, U(RejectLabel), Return, EndCode];
        byte[] input =
        [
            .. entry, .. Prefix(RetryLabel, first), .. firstTail,
            Label, U(RetryLabel), .. Prefix(RejectLabel, second), .. secondTail, .. fallback
        ];
        byte[] expected =
        [
            .. entry, IsNonemptyList, F(5), X(0), GetList, X(0), X(2), X(3),
            IsTuple, F(5), X(2), TestArity, F(5), X(2), U(2), GetTupleElement, X(2), U(0), X(2),
            SelectValue, X(2), F(5), ExtendedListOperand, U(4), A(first), F(3), A(second), F(4),
            Label, U(3), IsNil, F(5), X(3), Move, X(3), X(0), Return,
            Label, U(4), .. secondTail, Label, U(5), Return, EndCode
        ];
        byte[] observesSecondHead =
        [
            .. entry, .. Prefix(RetryLabel, first), .. firstTail, Label, U(RetryLabel),
            .. Prefix(RejectLabel, second), Move, X(2), X(0), Return, .. fallback
        ];
        byte[] observesFallbackTail =
        [
            .. entry, .. Prefix(RetryLabel, first), .. firstTail, Label, U(RetryLabel),
            .. Prefix(RejectLabel, second), .. secondTail, Label, U(RejectLabel), Move, X(3), X(0), Return, EndCode
        ];
        byte[] overwritesOriginalList =
        [
            .. entry, .. Prefix(RetryLabel, first), Move, X(3), X(0), .. firstTail,
            Label, U(RetryLabel), .. Prefix(RejectLabel, second), .. secondTail, .. fallback
        ];
        if (Apply(input) is not { } result || result.LabelCount != 6 || !result.Code.Span.SequenceEqual(expected)
            || Apply(observesSecondHead) is not null || Apply(observesFallbackTail) is not null
            || Apply(overwritesOriginalList) is not null)
        {
            throw new InvalidOperationException(WrongPath);
        }

        static byte[] Prefix(int failed, int tag) =>
        [
            IsNonemptyList, F(failed), X(0), GetList, X(0), X(2), X(3),
            IsTaggedTuple, F(failed), X(2), U(2), A(tag)
        ];

        (ReadOnlyMemory<byte> Code, int LabelCount)? Apply(byte[] body) =>
            ((ReadOnlyMemory<byte> Code, int LabelCount)?)Call(
                compiler,
                "SelectCheckedListDelimiterPrefixes",
                (ReadOnlyMemory<byte>)body,
                LabelCount
            );
    }

    private static void CheckRepeatedTaggedPrefix()
    {
        var compiler = new BeamModuleCompiler("tagged_prefix_retry");
        int function = (int)Call(compiler, "Intern", "match");
        int selected = (int)Call(compiler, "Intern", "selected");
        int other = (int)Call(compiler, "Intern", "other");
        byte[] entry =
        [
            .. Start(
                FunctionInfoLabel,
                EntryLabel,
                function,
                Arity
            ), IsTuple, F(RejectLabel), X(1), TestArity, F(RejectLabel), X(1), U(3)
        ];
        byte[] projections = [GetTupleElement, X(1), U(1), X(4), GetTupleElement, X(1), U(2), X(5)];
        byte[] success = [IsNil, F(RetryLabel), X(3), Move, X(2), X(0), Return, Label, U(RetryLabel)];
        byte[] tail = [Move, X(3), X(0), Return, Label, U(RejectLabel), Return, EndCode];
        byte[] original =
        [
            .. entry, .. projections, .. Prefix(RetryLabel, selected), .. success,
            .. projections, .. Prefix(RejectLabel, selected), .. tail
        ];
        byte[] expected =
        [
            .. entry, .. projections, .. Prefix(RejectLabel, selected), .. success,
            .. projections, GetList, X(0), X(2), X(3), .. tail
        ];
        byte[] anotherEntry =
        [
            .. entry, IsAtom, F(RetryLabel), X(1), .. projections, .. Prefix(RetryLabel, selected),
            .. success, .. projections, .. Prefix(RejectLabel, selected), .. tail
        ];
        byte[] changedTag =
        [
            .. entry, .. projections, .. Prefix(RetryLabel, selected), .. success,
            .. projections, .. Prefix(RejectLabel, other), .. tail
        ];
        byte[] changedList =
        [
            .. entry, .. projections, .. Prefix(RetryLabel, selected), Move, X(3), X(0), .. success,
            .. projections, .. Prefix(RejectLabel, selected), .. tail
        ];
        if (Apply(original) is not { } result || !result.AsSpan().SequenceEqual(expected)
            || Apply(anotherEntry) is not null || Apply(changedTag) is not null || Apply(changedList) is not null)
        {
            throw new InvalidOperationException(WrongPath);
        }

        static byte[] Prefix(int failed, int tag) =>
        [
            IsNonemptyList, F(failed), X(0), GetList, X(0), X(2), X(3),
            IsTaggedTuple, F(failed), X(2), U(2), A(tag)
        ];

        byte[]? Apply(byte[] body) => (byte[]?)Call(
            compiler,
            "ReuseCheckedListPrefixOnRetry",
            (ReadOnlyMemory<byte>)body,
            LabelCount
        );
    }

    private static byte[] Fixture(
        bool shared = false,
        bool otherPredecessor = false,
        bool overwriteState = false,
        bool observeSkippedRead = false,
        bool savedState = false
    )
    {
        byte[] additionalEdge = otherPredecessor ? [IsNil, F(RetryLabel), X(0)] : [];
        byte state = savedState ? Y(0) : X(1);
        byte[] frame = savedState ? [Allocate, U(1), U(2), Move, X(1), state] : [];
        byte[] release = savedState ? [Deallocate, U(1)] : [];
        byte[] clobber = overwriteState ? [GetTail, state, state] : [];
        byte[] repeated = shared ? [] : [IsNonemptyList, F(RejectLabel), state];
        byte[] stateResult = observeSkippedRead ? [] : [Move, state, X(0)];
        byte[] comparison = [GetTupleElement, X(0), U(1), X(0), ExactEqualTest, F(RejectLabel), X(0), I(7)];
        byte[] retry = savedState
            ? [.. repeated, GetList, state, X(2), X(3), .. comparison, Move, X(2), X(0)]
            : [.. comparison, .. repeated, GetList, state, X(0), X(3)];

        return
        [
            .. Start(
                FunctionInfoLabel,
                EntryLabel,
                2,
                Arity
            ),
            .. frame,
            IsTuple, F(RejectLabel), X(0), TestArity, F(RejectLabel), X(0), U(2),
            .. additionalEdge,
            IsNonemptyList, F(shared ? RejectLabel : RetryLabel), state, GetList, state, X(2), X(3),
            .. clobber,
            IsNonemptyList, F(RetryLabel), X(3), Move, X(3), X(0), .. release, Return,
            Label, U(RetryLabel),
            .. retry, .. release, Return,
            Label, U(RejectLabel), .. stateResult, .. release, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            LabelCount,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            8,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "MoveRejectedConsRetryEdge",
            code,
            body
        );
    }
}
