using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProjectedEntryCallArgumentChecks
{
    internal const string TestName = "compiler/beam/keeps-a-projected-call-argument-in-its-original-register";
    private const string RewriteMethod = "RewriteProjectedEntryCallArgument";
    private const string InvalidCopy = "The call argument no longer denotes the saved projection.";

    internal static void Run()
    {
        byte[] original = Code([Y(0)], []);
        byte[] expected = Code([X(2)], []);
        byte[] overwritten = Code([Y(0)], [Move, X(1), X(2)]);
        byte[] cleared = Code([Y(0)], [InitYRegisters, ExtendedListOperand, U(1), Y(0)]);
        if (Rewrite(original) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(overwritten) is not null || Rewrite(cleared) is not null)
        {
            throw new InvalidOperationException(InvalidCopy);
        }
    }

    private static byte[] Code(byte[] callArgument, byte[] intervening) =>
    [
        .. Start(
            1,
            2,
            1,
            2
        ),
        GetTupleElement, X(0), U(0), X(2),
        Allocate, U(2), U(3),
        Move, X(1), Y(1),
        Move, X(2), Y(0),
        .. intervening,
        Move, .. callArgument, X(0),
        Line, U(1),
        ExternalCall, U(1), U(0),
        Move, Y(0), X(0),
        Deallocate, U(2),
        Return,
        EndCode
    ];

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            3,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            RewriteMethod,
            code,
            body
        );
    }
}
