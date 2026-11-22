using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamAdjacentCopyReturnChecks
{
    internal const string TestName = "compiler/beam/shares-adjacent-frameless-copy-returns-by-final-label";
    private const string Unexpected = "Identical copy returns must share only the final compatible continuation.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("adjacent_copy_return_probe");
        if (Rewrite(Fixture(), 5) is not { Code: var code, LabelCount: 4 }
            || !code.Span.SequenceEqual(Fixture(shared: true))
            || Rewrite(code, 4) is not null || Rewrite(Fixture(otherInput: true), 5) is not null)
        {
            throw new InvalidOperationException(Unexpected);
        }

        return Task.CompletedTask;

        (ReadOnlyMemory<byte> Code, int LabelCount)? Rewrite(ReadOnlyMemory<byte> code, int labelCount) =>
            ((ReadOnlyMemory<byte>, int)?)Call(
                compiler,
                "OrderFinalPhysicalLabelsCore",
                code,
                labelCount,
                false,
                true,
                true
            );

        static byte[] Fixture(bool shared = false, bool otherInput = false)
        {
            byte[] duplicate = shared ? [] : [Label, U(3), Move, X(1), X(0), Return];

            return
            [
                .. Start(
                    1,
                    2,
                    1,
                    3
                ),
                ExactEqualTest, F(3), X(0), A(1), Jump, F(shared ? 3 : 4), .. duplicate,
                Label, U(shared ? 3 : 4), Move, X(otherInput ? 2 : 1), X(0), Return, EndCode
            ];
        }
    }
}
