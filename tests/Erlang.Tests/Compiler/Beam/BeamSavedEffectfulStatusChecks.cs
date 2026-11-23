using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSavedEffectfulStatusChecks
{
    internal const string TestName = "compiler/beam/partitions-saved-effectful-status-cases";
    private const string WrongPartition = "The saved status must retain its effect and case-failure locations.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_saved_effectful_status");
        _ = Call(compiler, "Intern", "ok");
        _ = Call(compiler, "Intern", "error");
        _ = Call(compiler, "Intern", "detail");
        if (Apply(Fixture()) is not ReadOnlyMemory<byte> partitioned
            || !partitioned.Span.SequenceEqual(Fixture(partition: true))
            || Apply(partitioned) is not null || Apply(Fixture(clobbersStatus: true)) is not null
            || Apply(Fixture(missingEffectLine: true)) is not null)
        {
            throw new InvalidOperationException(WrongPartition);
        }

        return Task.CompletedTask;

        object? Apply(ReadOnlyMemory<byte> code) => Call(
            compiler,
            "PartitionLateSavedEffectfulTupleReturns",
            code,
            5
        );
    }

    private static byte[] Fixture(bool partition = false, bool clobbersStatus = false, bool missingEffectLine = false)
    {
        byte[] atom = [Move, A(2), X(0), Deallocate, U(2), Return];
        byte[] tests = partition
            ? [IsTuple, F(3), Y(0), IsTaggedTuple, F(4), ExtendedTypedRegisterOperand, Y(0), U(1), U(2), A(3)]
            : [ExactEqualTest, F(3), Y(0), A(2), .. atom, Label, U(3), IsTaggedTuple, F(4), Y(0), U(2), A(3)];
        byte[] saved = clobbersStatus ? [Move, A(4), Y(0)] : [];
        byte[] effectLine = missingEffectLine ? [] : [Line, U(1)];
        byte[] atomTail = partition ? [Label, U(3), ExactEqualTest, F(4), Y(0), A(2), .. atom] : [];
        byte[] caseFailure = partition ? [Line, U(2), CaseEnd, Y(0)] : [Move, Y(0), X(0), Line, U(2), CaseEnd, X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1), Move, X(0), Y(0),
            .. tests, GetTupleElement, Y(0), U(1), X(0), .. saved, IsTuple, F(4), X(0), TestArity, F(4), X(0), U(2),
            TestHeap, U(4), U(1), GetTupleElement, X(0), U(0), X(1), GetTupleElement, X(0), U(1), X(0),
            PutList, X(0), A(0), X(0), PutList, X(1), X(0), X(1), Move, A(4), X(0),
            Move, Y(0), Y(1), TrimFrame, U(1), U(1), .. effectLine, ExternalCall, U(2), U(0),
            Move, Y(0), X(0), Deallocate, U(1), Return,
            .. atomTail, Label, U(4), .. caseFailure, EndCode
        ];
    }
}
