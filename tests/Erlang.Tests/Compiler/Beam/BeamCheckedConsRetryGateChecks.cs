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
                if (!expected.Equals(prior) || !expected.Equals(current))
                {
                    throw new InvalidOperationException(WrongPath);
                }
            }

            return Nil.Value;
        });
    }

    private static byte[] Fixture(
        bool shared = false,
        bool otherPredecessor = false,
        bool overwriteState = false,
        bool observeSkippedRead = false
    )
    {
        byte[] additionalEdge = otherPredecessor ? [IsNil, F(RetryLabel), X(0)] : [];
        byte[] clobber = overwriteState ? [GetTail, X(1), X(1)] : [];
        byte[] repeated = shared ? [] : [IsNonemptyList, F(RejectLabel), X(1)];
        byte[] stateResult = observeSkippedRead ? [] : [Move, X(1), X(0)];

        return
        [
            .. Start(
                FunctionInfoLabel,
                EntryLabel,
                2,
                Arity
            ),
            IsTuple, F(RejectLabel), X(0), TestArity, F(RejectLabel), X(0), U(2),
            .. additionalEdge,
            IsNonemptyList, F(shared ? RejectLabel : RetryLabel), X(1), GetList, X(1), X(2), X(3),
            .. clobber,
            IsNonemptyList, F(RetryLabel), X(3), Move, X(3), X(0), Return,
            Label, U(RetryLabel),
            GetTupleElement, X(0), U(1), X(0), ExactEqualTest, F(RejectLabel), X(0), I(7),
            .. repeated, GetList, X(1), X(0), X(3), Return,
            Label, U(RejectLabel), .. stateResult, Return, EndCode
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
