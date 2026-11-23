using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedEntryListHeadChecks
{
    internal const string TestName = "compiler/beam/removes-dead-list-heads-before-allocating-call-frames";
    private const string MissingRewrite = "Expected the checked dead head to be replaced by a tail projection and stack initialization.";
    private const string UnsafeRewrite = "Expected a live stack head, nonterminal failure or post-call destructuring to retain the original shape.";
    private const byte Zero = 0x00;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Function = 0x22;
    private const byte Module = 0x12;
    private const byte Entry = 0x25;
    private const byte Failed = 0x35;
    private const byte Matched = 0x45;

    internal static Task Run()
    {
        byte[] prefix = [Label, One, FunctionInfo, Module, Function, Two, Label, Two, Allocate, Two, Two];
        byte[] test = [IsNonemptyList, Failed, X1];
        byte[] project = [GetList, X1, Y0, Y1];
        byte[] tail = [GetTail, X1, Y1, InitYRegisters, ExtendedListOperand, One, Y0];
        byte[] failure = [Jump, Matched, Label, Three, Move, X1, X0, Line, One, BadMatch, X0, Label, Four];
        byte[] call = [Move, Y1, X0, Line, Two, LocalCall, One, Entry];
        byte[] exit = [Move, X0, X1, Move, X1, X0, Deallocate, Two, Return, EndCode];

        byte[] original = [.. prefix, .. test, .. project, .. failure, .. call, .. exit];
        byte[] expected = [.. prefix, .. test, .. tail, .. failure, .. call, .. exit];
        if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingRewrite);
        }

        byte[] liveHead = [.. prefix, .. test, .. project, .. failure, .. call, Move, Y0, X0, .. exit];
        byte[] liveXHead = [.. prefix, .. test, GetList, X1, X0, Y1, .. failure, .. call, .. exit];
        byte[] otherSource = [.. prefix, .. test, GetList, X0, Y0, Y1, .. failure, .. call, .. exit];
        byte[] callResultProjection =
        [
            .. prefix, .. test, .. project, .. failure, .. call,
            GetTupleElement, X0, Zero, X0, .. exit
        ];
        byte[] sharedFallback =
        [
            .. prefix, .. test, .. project, Jump, Matched,
            Label, Three, Move, X1, Y1, Jump, Matched, Label, Four, .. call, .. exit
        ];
        if (Rewrite(liveHead) is not null || Rewrite(liveXHead) is not null || Rewrite(otherSource) is not null
            || Rewrite(callResultProjection) is not null || Rewrite(sharedFallback) is not null)
        {
            throw new InvalidOperationException(UnsafeRewrite);
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
            5,
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
            "RemoveDeadCheckedEntryListHeads",
            instructions,
            body
        );
    }
}
