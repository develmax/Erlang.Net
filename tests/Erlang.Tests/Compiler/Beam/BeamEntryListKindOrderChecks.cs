using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryListKindOrderChecks
{
    internal const string TestName = "compiler/beam/orders-nonempty-list-before-empty-at-entry";
    private const string MissingOrder = "Expected the cons arm before the nil arm with the original function failure.";
    private const string UnsafeOrder = "A different input, shared arm, inherited exception location or handler must retain the original list block order.";
    private const byte Zero = 0x00;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte FunctionFailure = 0x15;
    private const byte SecondArm = 0x35;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte Empty = 0x32;
    private const byte Nonempty = 0x42;

    internal static Task Run()
    {
        byte[] entry = [Label, One, Line, One, FunctionInfo, Module, Function, One, Label, Two];
        byte[] nil = [Move, Empty, X0, Return];
        byte[] cons = [Move, Nonempty, X0, Return];
        byte[] source =
        [
            .. entry, IsNil, SecondArm, X0, .. nil,
            Label, Three, IsNonemptyList, FunctionFailure, X0, .. cons, EndCode
        ];
        byte[] expected =
        [
            .. entry, IsNonemptyList, SecondArm, X0, .. cons,
            Label, Three, IsNil, FunctionFailure, X0, .. nil, EndCode
        ];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingOrder);
        }

        byte[] nilCall = [Line, Two, Move, Empty, X0, TailExternalCall, One, Zero];
        byte[] consCall = [Line, Three, Move, Nonempty, X0, TailExternalCall, One, Zero];
        byte[] sourceWithLines =
        [
            .. entry, IsNil, SecondArm, X0, .. nilCall,
            Label, Three, IsNonemptyList, FunctionFailure, X0, .. consCall,
            Label, Four, Line, Four, Return, EndCode
        ];
        byte[] expectedWithLines =
        [
            .. entry, IsNonemptyList, SecondArm, X0, .. consCall,
            Label, Three, IsNil, FunctionFailure, X0, .. nilCall,
            Label, Four, Line, Four, Return, EndCode
        ];
        if (Rewrite(sourceWithLines) is not { } actualWithLines
            || !actualWithLines.AsSpan().SequenceEqual(expectedWithLines))
        {
            throw new InvalidOperationException(MissingOrder);
        }

        byte[] distinctInput =
        [
            .. entry, IsNil, SecondArm, X0, .. nil,
            Label, Three, IsNonemptyList, FunctionFailure, X1, .. cons, EndCode
        ];
        byte[] sharedArm = [.. source[..^1], Label, Four, Jump, SecondArm, EndCode];
        byte[] misplacedLine =
        [
            .. entry, IsNil, SecondArm, X0, .. nilCall,
            Label, Three, IsNonemptyList, FunctionFailure, X0, Move, Nonempty, X0, TailExternalCall, One, Zero,
            EndCode
        ];
        byte[] handler =
        [
            .. entry, IsNil, SecondArm, X0, .. nil,
            Label, Three, IsNonemptyList, FunctionFailure, X0, CatchEnter, Y0, FunctionFailure, .. cons, EndCode
        ];
        if (Rewrite(distinctInput) is not null || Rewrite(sharedArm) is not null
            || Rewrite(misplacedLine) is not null || Rewrite(handler) is not null)
        {
            throw new InvalidOperationException(UnsafeOrder);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            5,
            1,
            instructions
        );
        var labels = new List<(int Start, int End, int Label)>();
        Action<int, int, int> recordLabel = (start, end, label) => labels.Add((start, end, label));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            recordLabel,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "OrderEntryListKinds",
            instructions,
            body,
            labels
        );
    }
}
