using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectedCallArgumentCopyChecks
{
    internal const string TestName = "compiler/beam/keeps-projection-before-ordinary-argument-copies";
    private const string Failure = "Expected the checked tuple projection before the two direct ordinary argument copies.";
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte X3 = 0x33;
    private const byte Y0 = 0x04;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte Target = 0x35;

    internal static Task Run()
    {
        byte[] frame = [Label, One, FunctionInfo, Module, Function, Three, Label, Two, Allocate, One, Three];
        byte[] save = [Move, X2, X3];
        byte[] projection = [GetTupleElement, X0, Four, Y0];
        byte[] copies = [Move, Y0, X2, Move, X3, X0];
        byte[] finish = [Line, One, LocalCall, Three, Target, Deallocate, One, Return, EndCode];
        byte[] candidate = [.. frame, .. save, .. projection, .. copies, .. finish];
        byte[] expected = [.. frame, .. projection, Move, X2, X0, Move, Y0, X2, .. finish];
        if (Rewrite(candidate) is not { } result || !result.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(Failure);
        }

        byte[] liveScratch =
        [
            .. frame, .. save, .. projection, .. copies, ExternalCall, Four, Zero, Deallocate, One, Return, EndCode
        ];
        byte[] collecting = [.. frame, .. save, TestHeap, One, Four, .. projection, .. copies, .. finish];
        byte[] differentDestination = [.. frame, .. save, .. projection, Move, Y0, X1, Move, X3, X0, .. finish];
        if (Rewrite(liveScratch) is not null || Rewrite(collecting) is not null || Rewrite(differentDestination) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
            1,
            instructions
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
            "OrderIndependentCallArguments",
            instructions,
            body
        );
    }
}
