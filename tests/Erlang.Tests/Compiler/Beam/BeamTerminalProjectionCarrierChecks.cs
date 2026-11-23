using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalProjectionCarrierChecks
{
    internal const string TestName = "compiler/beam/reuses-a-checked-carrier-for-its-terminal-projection";
    private const string WrongLayout = "A terminal projection can reuse its carrier only after the heap and tuple checks.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_terminal_projection_carrier");
        _ = Call(compiler, "Intern", "subject");
        _ = Call(compiler, "Intern", "status");
        if (Apply(Fixture()) is not { } current || !current.SequenceEqual(Fixture(reuse: true))
            || Apply(current) is not null || Apply(Fixture(readCarrier: true)) is not null
            || Apply(Fixture(differentFailure: true)) is not null
            || Apply(Fixture(extraHeapRoot: true)) is not null
            || Apply(SiblingFixture()) is not { } withSibling
            || !withSibling.SequenceEqual(SiblingFixture(reuse: true))
            || Apply(SiblingFixture(cons: true)) is not { } cons
            || !cons.SequenceEqual(SiblingFixture(reuse: true, cons: true))
            || Apply(SiblingFixture(projectSibling: true, wrap: true)) is not { } wrapped
            || !wrapped.SequenceEqual(SiblingFixture(reuse: true, projectSibling: true, wrap: true))
            || Apply(SiblingFixture(readCarrier: true)) is not null
            || Apply(SiblingFixture(projectSibling: true, reconstructPair: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReuseTerminalProjectionCarrier",
            code,
            5
        );
    }

    private static byte[] Fixture(
        bool reuse = false,
        bool readCarrier = false,
        bool differentFailure = false,
        bool extraHeapRoot = false
    )
    {
        byte projected = X(reuse ? 0 : 1);

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTuple, F(3), X(0), TestArity, F(differentFailure ? 4 : 3), X(0), U(2),
            TestHeap, U(3), U(extraHeapRoot ? 2 : 1), GetTupleElement, X(0), U(1), projected,
            PutTuple, X(0), ExtendedListOperand, U(2), readCarrier ? X(0) : A(3), projected,
            Return, Label, U(3), Return, Label, U(4), Return, EndCode
        ];
    }

    private static byte[] SiblingFixture(
        bool reuse = false,
        bool cons = false,
        bool projectSibling = false,
        bool wrap = false,
        bool readCarrier = false,
        bool reconstructPair = false
    )
    {
        byte projected = X(reuse ? 0 : 2);
        byte carrier = X(0);
        byte sibling = X(1);
        byte other = readCarrier ? carrier : sibling;
        byte[] earlierRead = projectSibling ? [GetTupleElement, carrier, U(0), sibling] : [];
        byte[] construction = (cons, reconstructPair) switch
        {
            (true, _) => [PutList, projected, other, carrier],
            (_, true) => [PutTuple, carrier, ExtendedListOperand, U(2), other, projected],
            _ => [PutTuple, carrier, ExtendedListOperand, U(3), A(3), projected, other]
        };
        byte[] wrapper = wrap ? [PutList, carrier, A(0), carrier] : [];
        int heap = cons ? 2 : (reconstructPair ? 3 : 4) + (wrap ? 2 : 0);

        return
        [
            .. Start(
                1,
                2,
                1,
                projectSibling ? 1 : 2
            ),
            IsTuple, F(3), carrier, TestArity, F(3), carrier, U(reconstructPair ? 2 : 4),
            TestHeap, U(heap), U(projectSibling ? 1 : 2), .. earlierRead,
            GetTupleElement, carrier, U(1), projected, .. construction, .. wrapper,
            Return, Label, U(3), Return, EndCode
        ];
    }
}
