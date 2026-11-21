using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProjectedCallArgumentCopyChecks
{
    internal const string TestName = "compiler/beam/keeps-projection-before-ordinary-argument-copies";
    private const string Failure = "Expected the checked tuple projection before the two direct ordinary argument copies.";
    private const string SplitStackFailure = "Expected independent stack loads ordered by slot before the kill, with the final X0 load and call line unchanged.";
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

        CheckSplitStackArguments();

        return Task.CompletedTask;
    }

    private static void CheckSplitStackArguments()
    {
        byte[] prefix =
        [
            Label, One, FunctionInfo, Module, Module, Three, Label, Two,
            Allocate, Three, Three, Move, X0, Y(0), Move, X1, Y(1), Move, X2, Y(2)
        ];
        byte[] loadSecond = [Move, Y(2), X1];
        byte[] loadThird = [Move, Y(0), X2];
        byte[] kill = [InitYRegisters, ExtendedListOperand, One, Y(2)];
        byte[] finish =
        [
            Move, Y(1), X0, Line, One, LocalCall, Three, Target, Deallocate, Three, Return, EndCode
        ];
        byte[] original = [.. prefix, .. loadSecond, .. loadThird, .. kill, .. finish];
        byte[] expected = [.. prefix, .. loadThird, .. loadSecond, .. kill, .. finish];
        var compiler = new BeamModuleCompiler("final_split_stack_arguments");
        if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || RewriteFinal(original) is not byte[] actualFinal || !actualFinal.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(SplitStackFailure);
        }

        byte[] lineBetweenLoads = [.. prefix, .. loadSecond, Line, One, .. loadThird, .. kill, .. finish];
        byte[] sameSource = [.. prefix, .. loadSecond, Move, Y(2), X2, .. kill, .. finish];
        byte[] dependsOnX = [.. prefix, .. loadSecond, Move, X1, X2, .. kill, .. finish];
        byte[] longerRun = [.. prefix, Move, Y(1), X2, .. loadSecond, .. loadThird, .. kill, .. finish];
        if (Rewrite(expected) is not null || Rewrite(lineBetweenLoads) is not null || Rewrite(sameSource) is not null
            || Rewrite(dependsOnX) is not null || Rewrite(longerRun) is not null
            || RewriteFinal(expected) is not null || RewriteFinal(lineBetweenLoads) is not null
            || RewriteFinal(sameSource) is not null || RewriteFinal(dependsOnX) is not null
            || RewriteFinal(longerRun) is not null)
        {
            throw new InvalidOperationException(SplitStackFailure);
        }

        object? RewriteFinal(byte[] image) => Call(
            compiler,
            "OrderFinalSplitStackArguments",
            (ReadOnlyMemory<byte>)image,
            4
        );
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
