using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamJoinedLiteralCallArgumentsChecks
{
    internal const string TestName = "compiler/beam/forwards-joined-literal-through-exclusive-call-arms";
    private const string Unexpected = "Expected the literal in X0 to feed both calls and only the success exit to trim.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("joined_literal_fixture");
        int arithmetic = (int)Call(
            compiler,
            "Import",
            "erlang",
            "-",
            2
        )!;
        int query = (int)Call(
            compiler,
            "Import",
            "effects",
            "query",
            3
        )!;
        int effect = (int)Call(
            compiler,
            "Import",
            "effects",
            "consume",
            2
        )!;

        if (Rewrite(Fixture()) is not byte[] actual || !actual.SequenceEqual(Fixture(rewritten: true))
            || Rewrite(actual) is not null || Rewrite(Fixture(readsChoice: true)) is not null)
        {
            throw new InvalidOperationException(Unexpected);
        }

        return Task.CompletedTask;

        object? Rewrite(byte[] code) => Call(
            compiler,
            "ForwardJoinedLiteralCallArguments",
            new ReadOnlyMemory<byte>(code),
            8
        );

        byte[] Fixture(bool rewritten = false, bool readsChoice = false)
        {
            byte[] save = rewritten ? [] : [Move, X(0), Y(1)];
            byte[] kills = rewritten ? [InitYRegisters, ExtendedListOperand, U(3), Y(0), Y(1), Y(2)] : [];
            byte[] arm =
            [
                Move, rewritten ? X(0) : Y(1), X(1), Move, Y(2), X(2), .. kills,
                Move, Y(3), X(0), ExternalCall, U(3), U(query)
            ];
            byte[] trim = rewritten ? [TrimFrame, U(4), U(0)] : [];

            return
            [
                .. Start(
                    1,
                    2,
                    1,
                    4
                ),
                Allocate, U(4), U(4), Move, X(3), Y(0), Move, X(2), Y(1), Move, X(1), Y(2), Move, X(0), Y(3),
                ExactEqualTest, F(3), Y(1), A(1), Move, I(4), X(0), Jump, F(4),
                Label, U(3), Move, I(0), X(0), Label, U(4), .. save,
                ExactEqualTest, F(5), Y(0), A(1), .. arm, Jump, F(6), Label, U(5), .. arm,
                Label, U(6), IsGreaterOrEqual, F(7), X(0), I(0),
                GcBifTwo, F(0), U(1), U(arithmetic), X(0), I(3), X(1), Move, Y(3), X(0), .. trim,
                ExternalCall, U(2), U(effect), Move, A(1), X(0), Deallocate, U(rewritten ? 0 : 4), Return,
                Label, U(7), Move, readsChoice ? Y(0) : A(1), X(0), Deallocate, U(4), Return, EndCode
            ];
        }
    }
}
