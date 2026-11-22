using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProjectedArgumentCallRootsChecks
{
    internal const string TestName = "compiler/beam/clears-only-dead-stack-roots-after-projected-call-arguments";

    internal static Task Run()
    {
        byte[] clearAll = Fixture([Y(0), Y(1), Y(2)]);
        byte[] keepCarrier = Fixture([Y(0), Y(1)], useCarrier: true);
        if (Rewrite(Fixture()) is not { } all || !all.SequenceEqual(clearAll)
            || Rewrite(Fixture(useCarrier: true)) is not { } partial || !partial.SequenceEqual(keepCarrier)
            || Rewrite(clearAll) is not null || Rewrite(keepCarrier) is not null
            || Rewrite(Fixture(splitCarrier: true)) is not null
            || Rewrite(Fixture(unrelatedCall: true)) is not null)
        {
            throw new InvalidOperationException(
                "The argument projections must clear only stack roots whose old terms cannot be read on return."
            );
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        byte[]? clears = null,
        bool useCarrier = false,
        bool splitCarrier = false,
        bool unrelatedCall = false
    )
    {
        byte[] initialize = clears is { Length: > 0 }
            ? [InitYRegisters, ExtendedListOperand, U(clears.Length), .. clears] : [];
        byte[] post = useCarrier
            ? [Move, X(0), Y(0), GetTupleElement, Y(2), U(3), X(1), Move, Y(0), X(0)]
            : [Move, X(0), Y(2)];
        byte[] producer = unrelatedCall ? [ExternalCall, U(1), U(0)] : [LocalCall, U(1), F(5)];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Allocate, U(3), U(3), Move, X(0), Y(0), Move, X(1), Y(1), Move, X(2), Y(2),
            GetTupleElement, Y(2), U(0), X(0), Line, U(1), .. producer,
            GetTupleElement, Y(2), U(1), X(1),
            GetTupleElement, splitCarrier ? Y(0) : Y(2), U(2), X(2),
            Move, Y(0), X(0), Move, Y(1), X(3), .. initialize,
            Line, U(2), ExternalCall, U(4), U(0), .. post, Deallocate, U(3), Return,
            .. Start(
                4,
                5,
                1,
                1
            ),
            Return, EndCode
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
            2,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            1,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteProjectedArgumentCallSlotRoots",
            code,
            body
        );
    }
}
