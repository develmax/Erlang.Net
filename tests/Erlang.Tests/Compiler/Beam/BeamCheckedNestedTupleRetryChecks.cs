using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedNestedTupleRetryChecks
{
    internal const string TestName = "compiler/beam/shares-checked-prefix-before-nested-tuple-retry";
    private const string WrongLayout = "Only the nested pair checks should enter the retry after its common prefix is shared.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_checked_nested_tuple_retry");
        _ = Call(compiler, "Intern", "outer");
        _ = Call(compiler, "Intern", "selected");
        if (Apply(Fixture()) is not { } output || !output.SequenceEqual(Fixture(shared: true))
            || Apply(output) is not null || Apply(Fixture(otherLiteral: true)) is not null
            || Apply(Fixture(extraPredecessor: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        if (Order(ProjectionFixture()) is not { } scheduled || !scheduled.SequenceEqual(ProjectionFixture(ordered: true))
            || Order(scheduled) is not null || Order(ProjectionFixture(uncheckedChild: true)) is not null
            || Order(ProjectionFixture(overwrittenCarrier: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Order(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "OrderCheckedNestedTupleReads",
            code,
            5
        );

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ShareCheckedNestedTupleRetryPrefix",
            code,
            5
        );
    }

    private static byte[] ProjectionFixture(
        bool ordered = false,
        bool uncheckedChild = false,
        bool overwrittenCarrier = false
    )
    {
        byte[] outer =
        [
            GetTupleElement, ordered ? X(0) : Y(3), U(0), overwrittenCarrier ? Y(2) : Y(1),
            GetTupleElement, ordered ? X(0) : Y(3), U(1), Y(0)
        ];
        byte[] child =
        [
            GetTupleElement, ordered ? X(1) : Y(2), U(0), X(2),
            GetTupleElement, ordered ? X(1) : Y(2), U(1), X(3),
            GetTupleElement, ordered ? X(1) : Y(2), U(2), X(4)
        ];
        byte[] reads = ordered ? [.. child, .. outer] : [.. outer, .. child];
        byte[] innerArity = uncheckedChild ? [] : [TestArity, F(3), X(1), U(3)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsTuple, F(3), X(0), TestArity, F(3), X(0), U(4), GetTupleElement, X(0), U(3), X(1),
            IsTuple, F(3), X(1), .. innerArity, Allocate, U(4), U(2), Move, X(1), Y(2), Move, X(0), Y(3),
            .. reads, Move, Y(0), X(0), Deallocate, U(4), Return,
            Label, U(3), Return, EndCode
        ];
    }

    private static byte[] Fixture(bool shared = false, bool otherLiteral = false, bool extraPredecessor = false)
    {
        byte[] alternate = extraPredecessor ? [IsAtom, F(3), X(1)] : [];
        byte[] prefix = Prefix(F(shared ? 4 : 3), A(3));
        byte[] retry = shared ? [] : Prefix(F(4), A(otherLiteral ? 1 : 3));

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            .. alternate, .. prefix,
            IsTuple, F(3), X(2), TestArity, F(3), X(2), U(2),
            Move, X(2), X(0), Return,
            Label, U(3), .. retry, Move, X(2), X(0), Return,
            Label, U(4), Move, X(1), X(0), Return, EndCode
        ];

        static byte[] Prefix(byte failure, byte selected) =>
        [
            IsTaggedTuple, failure, X(0), U(3), A(2),
            GetTupleElement, X(0), U(1), X(2), ExactEqualTest, failure, X(2), selected,
            GetTupleElement, X(0), U(2), X(2)
        ];
    }
}
