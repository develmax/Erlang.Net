using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamKnownKeyFindReturnChecks
{
    internal const string TestName = "compiler/beam/partitions-keyfind-tuple-before-false-with-bound-key";
    private const string WrongLayout = "The known lookup partition must preserve the key match and original case failure.";

    internal static Task Run()
    {
        if (Rewrite(Fixture()) is not { } actual || !actual.AsSpan().SequenceEqual(Fixture(partitioned: true))
            || Rewrite(actual) is not null || Rewrite(Fixture(unknownImport: true)) is not null
            || Rewrite(Fixture(unknownPosition: true)) is not null || Rewrite(Fixture(sharedTupleLabel: true)) is not null
            || Rewrite(Fixture(alternateFailure: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool partitioned = false,
        bool unknownImport = false,
        bool unknownPosition = false,
        bool sharedTupleLabel = false,
        bool alternateFailure = false
    )
    {
        byte[] falseReturn = [Move, Y(1), X(0), Deallocate, U(2), Return];
        byte[] setup = partitioned
            ? [IsTuple, F(3), ExtendedTypedRegisterOperand, X(0), U(1)]
            : [ExactEqualTest, F(3), X(0), A(2), .. falseReturn, Label, U(3), IsTuple, F(5), X(0)];
        byte[] cold = partitioned ? [Label, U(3), .. falseReturn] : [];
        byte[] branch = sharedTupleLabel ? [ExactEqualTest, F(3), X(0), A(3)] : [];
        byte[] failure = alternateFailure ? [Move, A(3), X(0), Deallocate, U(2), Return] : [CaseEnd, X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(2), Move, X(0), Y(0), Move, A(3), Y(1), .. branch,
            Move, X(1), X(2), Move, unknownPosition ? X(0) : I(1), X(1), Line, U(1),
            ExternalCall, U(3), U(unknownImport ? 1 : 0), .. setup,
            TestArity, F(5), X(0), U(2), GetTupleElement, X(0), U(0), X(1),
            ExactEqualTest, F(5), X(1), Y(0), GetTupleElement, X(0), U(1), X(0),
            CatchEnter, Y(1), F(4), Label, U(4), CatchEnd, Y(1), Deallocate, U(2), Return,
            .. cold, Label, U(5), Line, U(2), .. failure, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            6,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            4,
            0,
            null,
            null
        );
        Func<int> resultType = () => 1;

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteKnownKeyFindReturns",
            code,
            body,
            0,
            2,
            resultType
        );
    }
}
