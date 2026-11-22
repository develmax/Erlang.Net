using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClosureResultCheckedArgumentChecks
{
    internal const string TestName = "compiler/beam/forwards-a-closure-call-result-to-checked-arguments";
    private const string Unexpected = "The closure result must remain rooted in both checked call branches.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("closure_result_arguments");
        int submit = (int)Call(
            compiler,
            "Import",
            "fixture",
            "submit",
            1
        )!;
        int apply = (int)Call(
            compiler,
            "Import",
            "fixture",
            "apply",
            2
        )!;
        int finish = (int)Call(
            compiler,
            "Import",
            "fixture",
            "finish",
            1
        )!;

        if (Rewrite(Fixture()) is not byte[] output || !output.SequenceEqual(Fixture(rewritten: true))
            || Rewrite(output) is not null || Rewrite(Fixture(wrongField: true)) is not null)
        {
            throw new InvalidOperationException(Unexpected);
        }

        return Task.CompletedTask;

        object? Rewrite(byte[] code) => Call(
            compiler,
            "ForwardClosureResultToCheckedArguments",
            new ReadOnlyMemory<byte>(code),
            4
        );

        byte[] Fixture(bool rewritten = false, bool wrongField = false)
        {
            byte[] initializers = rewritten ? [InitYRegisters, ExtendedListOperand, U(1), Y(2)] : [];
            byte[] save = rewritten ? [] : [Move, X(0), Y(2)];
            byte[] aboveHeap = rewritten ? [Move, X(0), X(1)] : [];
            byte[] loaded = rewritten ? [TrimFrame, U(3), U(0)] : [Move, Y(2), X(1)];
            byte[] cold = rewritten ? [] : [Move, Y(2), X(0)];

            return
            [
                .. Start(
                    1,
                    2,
                    1,
                    3
                ),
                Allocate, U(3), U(3), Move, X(0), Y(2), Move, X(1), Y(1), Move, X(2), Y(0),
                MakeFun, U(0), X(0), ExtendedListOperand, U(3), Y(2), Y(1), Y(0), .. initializers,
                ExternalCall, U(1), U(submit), .. save, ExactEqualTest, F(3), Y(0), A(1),
                IsAtom, F(3), Y(1), TestHeap, U(3), U(rewritten ? 1 : 0), .. aboveHeap,
                PutTuple, X(0), ExtendedListOperand, U(2), A(1), Y(wrongField ? 0 : 1), .. loaded,
                ExternalCall, U(2), U(apply), TailExternalCallWithFrame, U(1), U(finish), U(rewritten ? 0 : 3),
                Label, U(3), .. cold, TailExternalCallWithFrame, U(1), U(finish), U(3), EndCode
            ];
        }
    }
}
