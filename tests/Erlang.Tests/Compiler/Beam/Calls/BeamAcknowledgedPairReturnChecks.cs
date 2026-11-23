using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamAcknowledgedPairReturnChecks
{
    internal const string TestName = "compiler/beam/shares-the-rejections-of-an-acknowledged-pair";
    private const string WrongFlow = "The checked pair must be restored only on success, with both original rejects sharing one return.";

    internal static Task Run()
    {
        byte[] expected = Fixture(split: true);
        if (Rewrite(Fixture()) is not ValueTuple<byte[], int> actual
            || actual.Item2 != 1 || !actual.Item1.SequenceEqual(expected)
            || Rewrite(Fixture(checkArity: 1)) is not null
            || Rewrite(Fixture(unrelatedEdge: true)) is not null)
        {
            throw new InvalidOperationException(WrongFlow);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool split = false, bool unrelatedEdge = false, int checkArity = 0)
    {
        byte[] before = unrelatedEdge ? [ExactEqualTest, F(4), X(0), A(2)] : [];
        byte[] check = [IsTaggedTuple, F(3), X(0), U(2), A(2)];
        byte[] save = [Move, X(0), Y(0)];
        byte[] release = [Deallocate, U(1), Return];
        byte[] start = [.. before, ExternalCall, U(1), U(0), .. (split ? save : check), .. (split ? check : save)];
        byte[] finish = split ? [.. release, Label, U(3), .. release] : [Label, U(3), Label, U(4), .. release];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(1), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0),
            .. start, LocalCall, U(checkArity), F(2),
            ExactEqualTest, F(split ? 3 : 4), X(0), A(2), Move, Y(0), X(0),
            .. finish, EndCode
        ];
    }

    private static object? Rewrite(byte[] bytes)
    {
        var image = new ReadOnlyMemory<byte>(bytes);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            5,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return Call(
            typeof(BeamModuleCompiler),
            "RewriteAcknowledgedPairReturns",
            image,
            body
        );
    }
}
