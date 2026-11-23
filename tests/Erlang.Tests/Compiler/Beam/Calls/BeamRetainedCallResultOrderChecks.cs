using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetainedCallResultOrderChecks
{
    internal const string TestName = "compiler/beam/orders-independent-arguments-around-a-retained-call-result";
    private const string Failure = "The result save must follow the last read of its retained Y slot.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_retained_call_result_order");
        byte[] entry =
        [
            .. Start(
                1,
                2,
                1,
                4
            ),
            Allocate, U(4), U(4), Move, X(0), Y(0), Move, X(1), Y(1), Move, X(2), Y(2), Move, X(3), Y(3),
            ExternalCall, U(1), U(0)
        ];
        byte[] after =
        [
            TrimFrame, U(3), U(1), Move, X(4), X(0), Line, U(1),
            ExternalCall, U(4), U(0), Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
        ];
        byte[] current =
        [
            .. entry, Move, Y(3), X(4), Move, Y(2), X(1), Move, Y(1), X(2), Move, Y(0), X(3), Move, X(0), Y(3),
            .. after
        ];
        byte[] expected =
        [
            .. entry, Move, Y(1), X(2), Move, Y(2), X(1), Move, Y(3), X(4), Move, X(0), Y(3), Move, Y(0), X(3),
            .. after
        ];
        byte[] overwritten =
        [
            .. entry, Move, Y(2), X(1), Move, Y(1), X(2), Move, Y(0), X(3), Move, X(0), Y(3), Move, Y(3), X(4),
            .. after
        ];
        byte[] duplicate =
        [
            .. entry, Move, Y(3), X(4), Move, Y(2), X(1), Move, Y(2), X(2), Move, Y(0), X(3), Move, X(0), Y(3),
            .. after
        ];
        if (Rewrite(current) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(overwritten) is not null || Rewrite(duplicate) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "OrderArgumentsWithRetainedCallResult",
            code,
            3
        );
    }
}
