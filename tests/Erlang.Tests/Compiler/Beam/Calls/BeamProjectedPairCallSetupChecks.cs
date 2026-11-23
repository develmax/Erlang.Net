using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProjectedPairCallSetupChecks
{
    internal const string TestName = "compiler/beam/routes-returned-pair-fields-to-next-call-and-retained-slot";
    private const string WrongLayout = "The outgoing pair lost its permanent field, argument, or temporary stack clear.";

    internal static Task Run()
    {
        if (Rewrite(Fixture()) is not { } actual || !actual.AsSpan().SequenceEqual(Fixture(rewritten: true))
            || Rewrite(actual) is not null || Rewrite(Fixture(observeOldX0: true)) is not null
            || Rewrite(Fixture(clear: false)) is not null || Rewrite(Fixture(overwriteArgument: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool rewritten = false,
        bool observeOldX0 = false,
        bool clear = true,
        bool overwriteArgument = false
    )
    {
        byte[] capture = rewritten
            ? [GetTupleElement, X(0), U(0), Y(1), GetTupleElement, X(0), U(1), X(2)]
            : [
                GetTupleElement, X(0), U(0), X(2), GetTupleElement, X(0), U(1), X(0),
                Move, X(0), Y(0), Move, X(2), Y(1), Move, Y(0), X(2)
            ];
        byte[] clearTransit = clear ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];
        byte[] argumentOverwrite = overwriteArgument ? [Move, A(2), X(2)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Allocate, U(2), U(3), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            LocalCall, U(3), F(4), .. capture,
            Move, observeOldX0 ? X(0) : A(1), X(1), Move, A(2), X(0), .. argumentOverwrite,
            .. clearTransit, Line, U(1), LocalCall, U(3), F(4),
            TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), Y(1), X(0),
            Deallocate, U(2), Return,
            .. Start(
                3,
                4,
                2,
                3
            ),
            TestHeap, U(3), U(3), PutTuple, X(0), ExtendedListOperand, U(2), X(0), X(2),
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
            5,
            2,
            code
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
            "RewriteProjectedPairCallSetup",
            code,
            body
        );
    }
}
