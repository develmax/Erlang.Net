using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamExhaustiveComparisonBranchChecks
{
    internal const string TestName = "compiler/beam/folds-dead-comparison-values-into-branches";
    private const string WrongBranch = "Comparison branching must preserve operands, both successors, and live boolean results.";

    internal static Task Run()
    {
        foreach (bool firstTrue in new[] { true, false })
        {
            byte[] original = Fixture(firstTrue);
            if (Fold(original) is not ValueTuple<byte[], int> result
                || result.Item2 != 2 || !result.Item1.SequenceEqual(Expected(firstTrue))
                || Fold(Fixture(firstTrue, resultUsed: true)) is not ValueTuple<byte[], int> retained
                || retained.Item2 != 2 || !retained.Item1.SequenceEqual(Expected(firstTrue, resultUsed: true))
                || Fold(Fixture(firstTrue, sharedFailure: true)) is not ValueTuple<byte[], int> shared
                || shared.Item2 != 1 || !shared.Item1.SequenceEqual(Expected(firstTrue, sharedFailure: true))
                || Fold(Fixture(firstTrue, resultUsed: true, sharedUse: true)) is not null)
            {
                throw new InvalidOperationException(WrongBranch);
            }
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool firstTrue,
        bool resultUsed = false,
        bool sharedFailure = false,
        bool sharedUse = false
    )
    {
        byte[] firstArm = resultUsed ? [Return] : [Move, A(3), X(0), Return];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            .. OtherEdge(sharedFailure, sharedUse),
            BifTwo, F(0), U(0), X(0), X(1), X(0),
            SelectValue, X(0), F(5), ExtendedListOperand, U(4),
            A(3), F(firstTrue ? 4 : 3), A(4), F(firstTrue ? 3 : 4),
            Label, U(3), .. firstArm,
            Label, U(4), Move, A(4), X(0), Return,
            Label, U(5), CaseEnd, X(0), EndCode
        ];
    }

    private static byte[] Expected(bool firstTrue, bool resultUsed = false, bool sharedFailure = false)
    {
        byte[] firstArm = resultUsed ? [Move, A(firstTrue ? 4 : 3), X(0), Return] : [Move, A(3), X(0), Return];
        byte[] cold = sharedFailure ? [Label, U(5), CaseEnd, X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            .. OtherEdge(sharedFailure, sharedUse: false),
            firstTrue ? IsLess : IsGreaterOrEqual, F(4), X(1), X(0),
            .. firstArm, Label, U(4), Move, A(4), X(0), Return, .. cold, EndCode
        ];
    }

    private static byte[] OtherEdge(bool sharedFailure, bool sharedUse) => sharedFailure || sharedUse
        ? [IsAtom, F(sharedFailure ? 5 : 3), X(1)] : [];

    private static object? Fold(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            6,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            null,
            null
        );

        return Call(
            typeof(BeamModuleCompiler),
            "RewriteExhaustiveComparisonBranches",
            image,
            body,
            new Dictionary<int, (byte, bool)> { [0] = (IsLess, true) },
            3,
            4
        );
    }
}
