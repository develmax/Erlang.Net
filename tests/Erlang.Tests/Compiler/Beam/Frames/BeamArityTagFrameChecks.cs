using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamArityTagFrameChecks
{
    internal const string TestName = "compiler/beam/checks-an-arity-arm-tag-before-its-frame";
    private const string MissingGuard = "Expected the arity proof and tag guard before allocation with the original function clause arguments.";
    private const string ChangedFallback = "A surviving tag, width mismatch, alternate entry, handler or intervening call must keep the original frame boundary.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Y3 = 0x34;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Five = 0x50;
    private const byte FunctionFailure = 0x15;
    private const byte LargeArm = 0x35;
    private const byte SmallArm = 0x45;
    private const byte DiscardFrame = 0x55;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte Tag = 0x32;

    internal static Task Run()
    {
        byte[] dispatch =
        [
            Label, One,
            FunctionInfo, Module, Function, One,
            Label, Two,
            IsTuple, FunctionFailure, X0,
            SelectTupleArity, X0, FunctionFailure, ExtendedListOperand, Four, Two, SmallArm, Three, LargeArm,
            Label, Three
        ];
        byte[] frame = [Allocate, Three, One, InitYRegisters, ExtendedListOperand, One, Y2];
        byte[] original =
        [
            .. frame,
            Move, X0, Y0,
            GetTupleElement, Y0, Zero, Y1,
            ExactEqualTest, DiscardFrame, Y1, Tag
        ];
        byte[] lowered =
        [
            GetTupleElement, X0, Zero, X1,
            ExactEqualTest, FunctionFailure, X1, Tag,
            Allocate, Three, One,
            InitYRegisters, ExtendedListOperand, Two, Y1, Y2,
            Move, X0, Y0
        ];
        byte[] tail = [Move, Y0, X0, TailExternalCallWithFrame, One, Zero, Three];
        byte[] cleanup = [Label, Five, Move, Y0, X0, Deallocate, Three];
        byte[] functionClause = [Jump, FunctionFailure, EndCode];
        byte[] source =
        [
            .. dispatch, .. original, .. tail,
            Label, Four, .. original, .. tail,
            .. cleanup, .. functionClause
        ];
        byte[] expected =
        [
            .. dispatch, .. lowered, .. tail,
            Label, Four, .. lowered, .. tail,
            .. cleanup, .. functionClause
        ];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingGuard);
        }

        byte[] smallLeaf = [Label, Four, Return];
        byte[] usesTag =
        [
            .. dispatch, .. original, Move, Y1, X0, TailExternalCallWithFrame, One, Zero, Three,
            .. smallLeaf, .. cleanup, .. functionClause
        ];
        byte[] alternate = [.. dispatch, .. original, .. tail, .. smallLeaf, .. cleanup, Return, EndCode];
        byte[] wrongWidth =
        [
            .. dispatch, .. original, .. tail, Label, Four, .. original, .. tail,
            Label, Five, Move, Y0, X0, Deallocate, Two, .. functionClause
        ];
        byte[] crossEntry =
        [
            .. dispatch, .. original, .. tail,
            Label, Four, Jump, LargeArm, .. cleanup, .. functionClause
        ];
        byte[] handler =
        [
            .. dispatch, .. original, CatchEnter, Y2, DiscardFrame, .. tail,
            .. smallLeaf, .. cleanup, .. functionClause
        ];
        byte[] callBeforeProjection =
        [
            .. dispatch, .. frame, ExternalCall, One, Zero, .. original[frame.Length..], .. tail,
            .. smallLeaf, .. cleanup, .. functionClause
        ];
        if (Rewrite(usesTag) is not null || Rewrite(alternate) is not null || Rewrite(wrongWidth) is not null
            || Rewrite(crossEntry) is not null || Rewrite(handler) is not null || Rewrite(callBeforeProjection) is not null)
        {
            throw new InvalidOperationException(ChangedFallback);
        }

        CheckMultipleSavedArguments();

        return Task.CompletedTask;
    }

    private static void CheckMultipleSavedArguments()
    {
        byte[] dispatch =
        [
            Label, One,
            FunctionInfo, Module, Function, Two,
            Label, Two,
            IsTuple, FunctionFailure, X0,
            SelectTupleArity, X0, FunctionFailure, ExtendedListOperand, Four, Two, SmallArm, Three, LargeArm,
            Label, Three
        ];
        byte[] saves = [Move, X1, Y0, Move, X0, Y1];
        byte[] frame = [Allocate, Four, Two, InitYRegisters, ExtendedListOperand, One, Y3];
        byte[] tag = [GetTupleElement, Y1, Zero, Y2, ExactEqualTest, DiscardFrame, Y2, Tag];
        byte[] success =
        [
            Move, Y1, X0,
            Move, Y0, X1,
            TailExternalCallWithFrame, Two, Zero, Four,
            Label, Four, Return
        ];
        byte[] release = [Deallocate, Four, Jump, FunctionFailure, EndCode];
        byte[] restores = [Move, Y0, X1, Move, Y1, X0];
        byte[] source =
        [
            .. dispatch, .. frame, .. saves, .. tag, .. success,
            Label, Five, .. restores, .. release
        ];
        byte[] expected =
        [
            .. dispatch,
            GetTupleElement, X0, Zero, X2,
            ExactEqualTest, FunctionFailure, X2, Tag,
            Allocate, Four, Two,
            InitYRegisters, ExtendedListOperand, Two, Y2, Y3,
            .. saves, .. success,
            Label, Five, .. restores, .. release
        ];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingGuard);
        }

        byte[] swappedRestore =
        [
            .. dispatch, .. frame, .. saves, .. tag, .. success,
            Label, Five, Move, Y0, X0, Move, Y1, X1, .. release
        ];
        byte[] guardOverwritesSavedArgument =
        [
            .. dispatch, .. frame, .. saves,
            GetTupleElement, Y1, Zero, Y0, ExactEqualTest, DiscardFrame, Y0, Tag,
            .. success, Label, Five, .. restores, .. release
        ];
        if (Rewrite(swappedRestore) is not null || Rewrite(guardOverwritesSavedArgument) is not null)
        {
            throw new InvalidOperationException(ChangedFallback);
        }
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            6,
            1,
            instructions
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            4,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "DelayArityTagFrames",
            instructions,
            body
        );
    }
}
