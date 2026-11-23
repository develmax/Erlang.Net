using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamZeroFrameArithmeticResultChecks
{
    internal const string TestName = "compiler/beam/forwards-an-arithmetic-argument-across-a-zero-frame";
    private const string Incorrect = "The arithmetic argument must replace the dead incoming register and keep its failure operand.";
    private const string Unsafe = "A guarded arithmetic result or live scratch must retain its original allocation roots.";
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte IntegerTwo = 0x21;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte NoFailure = 0x05;
    private const byte BranchFailure = 0x35;

    internal static Task Run()
    {
        byte[] entry = [Label, One, FunctionInfo, Module, Function, One, Label, Two, Line, One];
        byte[] arithmetic = [GcBifTwo, NoFailure, One, Zero, X0, IntegerTwo, X1];
        byte[] frameAndCopy = [Allocate, Zero, Two, Move, X1, X0];
        byte[] argument = [Move, IntegerTwo, X1];
        byte[] callAndReturn = [ExternalCall, Two, One, Deallocate, Zero, Return, EndCode];
        byte[] source = [.. entry, .. arithmetic, .. frameAndCopy, .. argument, .. callAndReturn];
        byte[] expected =
        [
            .. entry, GcBifTwo, NoFailure, One, Zero, X0, IntegerTwo, X0,
            Allocate, Zero, One, .. argument, .. callAndReturn
        ];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(Incorrect);
        }

        byte[] liveScratch = [.. entry, .. arithmetic, .. frameAndCopy, .. callAndReturn];
        byte[] guarded =
        [
            .. entry, GcBifTwo, BranchFailure, One, Zero, X0, IntegerTwo, X1,
            .. frameAndCopy, .. argument, ExternalCall, Two, One, Deallocate, Zero, Return,
            Label, 0x30, Return, EndCode
        ];
        if (Rewrite(liveScratch) is not null || Rewrite(guarded) is not null)
        {
            throw new InvalidOperationException(Unsafe);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
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
            "StoreArithmeticResultAcrossZeroFrame",
            instructions,
            body,
            new Func<object, bool>(static _ => true)
        );
    }
}
