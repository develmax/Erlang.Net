using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetainedNestedStatusChecks
{
    internal const string TestName = "compiler/beam/retains-original-status-before-a-nested-tag-dispatch";
    private const string WrongLayout = "Nested status tests must preserve the original value on both rejection edges.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_retained_nested_status");
        _ = Call(compiler, "Intern", "ok");
        _ = Call(compiler, "Intern", "error");
        _ = Call(compiler, "Intern", "retry");
        if (Apply(Fixture()) is not ValueTuple<ReadOnlyMemory<byte>, int> changed
            || changed.Item2 != 6 || !changed.Item1.Span.SequenceEqual(Fixture(partitioned: true))
            || Apply(Fixture(readsOldSlot: true)) is not null
            || Apply(Fixture(passesOriginal: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        object? Apply(ReadOnlyMemory<byte> code) => Call(
            compiler,
            "OrderRetainedNestedStatusChecks",
            code,
            5
        );
    }

    private static byte[] Fixture(bool partitioned = false, bool readsOldSlot = false, bool passesOriginal = false)
    {
        byte[] tests = partitioned
            ? [
                Move, X(0), Y(0), IsTuple, F(3), X(0),
                IsTaggedTuple, F(4), ExtendedTypedRegisterOperand, X(0), U(1), U(2), A(3),
                GetTupleElement, X(0), U(1), X(0), IsTaggedTuple, F(4), X(0), U(2), A(4)
            ]
            : [
                ExactNotEqualTest, F(4), X(0), A(2), IsTaggedTuple, F(3), X(0), U(2), A(3),
                GetTupleElement, X(0), U(1), X(1), IsTaggedTuple, F(3), X(1), U(2), A(4), Move, X(0), Y(0)
            ];
        byte[] fallback = partitioned
            ? [Label, U(3), ExactNotEqualTest, F(5), X(0), A(2), Label, U(4), Move, Y(0), X(0), Label, U(5)]
            : [Label, U(3), Move, X(0), Y(0), Label, U(4)];
        byte[] suffix = readsOldSlot ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            Line, U(1), ExternalCall, U(1), U(0), .. tests,
            Line, U(2), ExternalCall, U(passesOriginal ? 1 : 0), U(1), Jump, F(partitioned ? 5 : 4),
            .. fallback, Move, X(0), Y(1), .. suffix, Deallocate, U(2), Return, EndCode
        ];
    }
}
