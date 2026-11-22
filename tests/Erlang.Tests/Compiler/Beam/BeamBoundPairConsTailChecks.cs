using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamBoundPairConsTailChecks
{
    internal const string TestName = "compiler/beam/retains-bound-pair-field-and-defers-terminal-tuple";
    private const string WrongLayout = "The bounded terminal constructor must preserve both calls and the live pair field.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_bound_pair_cons_tail");
        _ = Call(compiler, "Intern", "tuple_tag");
        if (Apply(Fixture()) is not { } changed || !changed.SequenceEqual(Fixture(scheduled: true))
            || Apply(changed) is not null || Apply(Fixture(tupleNeedsTail: true)) is not null
            || Apply(Fixture(priorUse: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ScheduleBoundPairConsTail",
            code,
            4
        );
    }

    private static byte[] Fixture(bool scheduled = false, bool tupleNeedsTail = false, bool priorUse = false)
    {
        int width = scheduled ? 3 : 4;
        byte[] used = priorUse ? [Move, X(1), Y(3)] : [];
        byte[] clear = scheduled ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(3)];
        byte[] pair = scheduled ? [] : [Move, X(0), Y(0), Move, X(1), Y(3)];
        byte[] tail = scheduled
            ?
            [
                Swap, Y(0), X(0), ExternalCall, U(1), U(1),
                TestHeap, U(9), U(1), PutTuple, X(1), ExtendedListOperand, U(4), A(2), Y(0), A(1), Y(1),
                PutList, X(1), X(0), X(0), PutList, Y(2), X(0), X(0), Deallocate, U(3)
            ]
            :
            [
                TestHeap, U(5), U(1), PutTuple, Y(0), ExtendedListOperand, U(4), A(2), X(0), A(1), tupleNeedsTail ? Y(3) : Y(1),
                Move, Y(3), X(0), Move, Y(0), Y(3), TrimFrame, U(2), U(2), ExternalCall, U(1), U(1),
                TestHeap, U(4), U(1), PutList, Y(1), X(0), X(0), PutList, Y(0), X(0), X(0), Deallocate, U(2)
            ];

        return
        [
            .. Start(
                1,
                2,
                1,
                4
            ),
            Allocate, U(width), U(4), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            Move, X(3), Y(1), Move, X(1), Y(2), .. used,
            Move, A(1), X(3), Move, A(0), X(1), Swap, X(0), X(2), .. clear,
            Line, U(1), LocalCall, U(4), F(2),
            GetTupleElement, X(0), U(0), scheduled ? Y(0) : X(1),
            GetTupleElement, X(0), U(1), X(0), .. pair,
            ExactEqualTest, F(3), X(0), A(1),
            TestHeap, U(2), U(0), PutList, A(2), Y(1), X(0), Move, X(0), Y(1),
            Label, U(3), Move, I(0), X(0), Line, U(1), ExternalCall, U(1), U(0),
            .. tail, Return, EndCode
        ];
    }
}
