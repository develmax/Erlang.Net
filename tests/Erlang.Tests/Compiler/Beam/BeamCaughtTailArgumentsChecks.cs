using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCaughtTailArgumentsChecks
{
    internal const string TestName = "compiler/beam/forwards-caught-value-to-shared-tail-arguments";
    private const string WrongLayout = "Both exception branches and the successful call must pass their original result to the shared tail.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_caught_tail_arguments");
        _ = Call(compiler, "Intern", "throw");
        _ = Call(compiler, "Intern", "exit_value");
        _ = Call(compiler, "Intern", "wrapped");
        if (Apply(Fixture(), 6) is not ValueTuple<ReadOnlyMemory<byte>, int> forwarded
            || forwarded.Item2 != 5 || !forwarded.Item1.Span.SequenceEqual(Fixture(forwarded: true))
            || Apply(forwarded.Item1, 5) is not null || Apply(Fixture(usesTrace: true), 6) is not null
            || Apply(Fixture(observesNonreceiver: true), 6) is not null
            || Apply(Fixture(missingProtectedLine: true), 6) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        object? Apply(ReadOnlyMemory<byte> code, int labels) => Call(
            compiler,
            "KeepCaughtResultInTailArguments",
            code,
            labels
        );
    }

    private static byte[] Fixture(
        bool forwarded = false,
        bool usesTrace = false,
        bool observesNonreceiver = false,
        bool missingProtectedLine = false
    )
    {
        byte[] header =
        [
            .. Start(
                1,
                2,
                1,
                4
            )
        ];
        byte[] callLocation = missingProtectedLine ? [] : [Line, U(1)];
        byte receiver = X(observesNonreceiver ? 2 : 3);
        byte reason = Y(usesTrace ? 0 : 1);
        if (forwarded)
        {
            return
            [
                .. header, Allocate, U(2), U(4), Move, X(3), Y(0), TryEnter, Y(1), F(3),
                .. callLocation, ExternalCall, U(3), U(0), TryEnd, Y(1), Move, X(0), X(1), Jump, F(4),
                Label, U(3), TryCase, Y(1), ExactNotEqualTest, F(4), ExtendedTypedRegisterOperand, X(0), U(1), A(2),
                TestHeap, U(6), U(2), PutTuple, X(0), ExtendedListOperand, U(2), A(3), X(1),
                PutTuple, X(1), ExtendedListOperand, U(2), A(4), X(0), Label, U(4),
                Move, Y(0), X(0), Line, U(2), TailExternalCallWithFrame, U(2), U(1), U(2), EndCode
            ];
        }

        return
        [
            .. header, Allocate, U(4), U(4), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            Move, receiver, Y(2), TryEnter, Y(3), F(3),
            .. callLocation, ExternalCall, U(3), U(0), TryEnd, Y(3), Jump, F(5),
            Label, U(3), TryCase, Y(3), Move, X(1), Y(1), Move, X(0), Y(3), Move, X(2), X(0), Move, X(0), Y(0),
            ExactEqualTest, F(4), Y(3), A(2), Move, Y(1), X(0), Jump, F(5),
            Label, U(4), TestHeap, U(6), U(0), PutTuple, X(0), ExtendedListOperand, U(2), A(3), reason,
            PutTuple, X(0), ExtendedListOperand, U(2), A(4), X(0), Label, U(5),
            Move, X(0), Y(3), Move, Y(3), X(1), Move, Y(2), X(0),
            Line, U(2), TailExternalCallWithFrame, U(2), U(1), U(4), EndCode
        ];
    }
}
