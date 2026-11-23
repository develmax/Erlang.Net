using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamStackFieldBeforeListConstructionChecks
{
    internal const string TestName = "compiler/beam/schedules-an-independent-stack-field-before-a-list-head";
    private const string WrongLayout = "The tuple head must keep its original operands when the tail projection is moved.";

    internal static Task Run()
    {
        if (Rewrite(Layout(scheduled: false)) is not { } result
            || !result.SequenceEqual(Layout(scheduled: true))
            || Rewrite(Layout(scheduled: false, headReadsPreviousTail: true)) is not null
            || Rewrite(result) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Layout(bool scheduled, bool headReadsPreviousTail = false)
    {
        byte[] tuple = [PutTuple, X(0), ExtendedListOperand, U(2), X(0), headReadsPreviousTail ? X(1) : A(1)];
        byte[] project = [GetTupleElement, Y(0), U(0), X(1)];
        byte[] middle = scheduled ? [.. project, .. tuple] : [.. tuple, .. project];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            Allocate, U(1), U(2), Move, X(0), Y(0), TestHeap, U(5), U(2),
            .. middle, PutList, X(0), X(1), X(0), Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            3,
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

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteStackFieldBeforeListConstruction",
            image,
            body
        );
    }
}
