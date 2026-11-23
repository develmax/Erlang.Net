using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalClosureCaptureChecks
{
    internal const string TestName = "compiler/beam/keeps-terminal-closure-captures-in-call-result-register";
    private const string WrongCapture = "The terminal closure capture lost its call result or heap root.";
    private const byte One = 0x10;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;

    internal static Task Run()
    {
        byte[] entryCall = [Label, One, Allocate, One, 0x00, ExternalCall, 0x00, 0x00];
        byte[] spill = [Move, X0, Y0];
        byte[] finish = [Deallocate, One, Return, EndCode];
        byte[] normal = [MakeFun, 0x00, X0, ExtendedListOperand, One, Y0];
        byte[] typed = [MakeFun, 0x00, X0, ExtendedListOperand, One, ExtendedTypedRegisterOperand, Y0, One];
        byte[] expected = [.. entryCall, TestHeap, One, One, MakeFun, 0x00, X0, ExtendedListOperand, One, X0, .. finish];
        byte[] expectedTyped =
        [
            .. entryCall, TestHeap, One, One, MakeFun, 0x00, X0, ExtendedListOperand, One,
            ExtendedTypedRegisterOperand, X0, One, .. finish
        ];
        if (Rewrite([.. entryCall, .. spill, TestHeap, One, 0x00, .. normal, .. finish]) is not { } actual
            || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite([.. entryCall, .. spill, TestHeap, One, 0x00, .. typed, .. finish]) is not { } actualTyped
            || !actualTyped.AsSpan().SequenceEqual(expectedTyped)
            || Rewrite([.. entryCall, .. spill, TestHeap, One, 0x00, MakeFun, 0x00, X1, ExtendedListOperand, One, Y0, .. finish]) is not null)
        {
            throw new InvalidOperationException(WrongCapture);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] instructions)
    {
        ReadOnlyMemory<byte> code = instructions;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)MakeFun,
            2,
            1,
            code
        );
        var slots = new List<(int Start, int End, int Slot)>();
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            (Action<int, int, int>)((start, end, slot) => slots.Add((start, end, slot)))
        );

        return (byte[]?)Call(
            new BeamModuleCompiler("terminal_closure_capture_rewrite"),
            "KeepTerminalCallResultsInX0",
            code,
            body,
            slots
        );
    }
}
