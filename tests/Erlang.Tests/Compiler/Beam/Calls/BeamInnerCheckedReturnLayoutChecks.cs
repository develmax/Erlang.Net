using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamInnerCheckedReturnLayoutChecks
{
    internal const string TestName = "compiler/beam/orders-an-inner-checked-return-before-outer-exits";
    private const string WrongLayout = "A checked return or enclosing exit changed its arguments, labels, or source line.";
    private const byte X0 = 0x03;
    private const byte Y0 = 0x04;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Five = 0x50;
    private const byte Six = 0x60;
    private const byte Seven = 0x70;
    private const byte Eight = 0x80;
    private const byte AtomOne = 0x12;
    private const byte AtomTwo = 0x22;
    private const byte AtomThree = 0x32;
    private const byte CaseFailure = 0x35;
    private const byte LiteralExit = 0x45;
    private const byte ValueExit = 0x55;
    private const byte CheckedReturn = 0x65;

    internal static Task Run()
    {
        byte[] input = Image(ordered: false);
        byte[] expected = Image(ordered: true);
        if (Rewrite(input) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(Image(ordered: false, laterExitEdge: true)) is not null
            || Rewrite(Image(ordered: false, sharedReturn: true)) is not null
            || Rewrite(Image(ordered: false, unlocatedExit: true)) is not null
            || Rewrite(expected) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(
        bool ordered,
        bool laterExitEdge = false,
        bool sharedReturn = false,
        bool unlocatedExit = false
    )
    {
        byte[] inner = [Label, Six, Deallocate, One, Return];
        byte[] exitLine = unlocatedExit ? [] : [Line, Two];
        byte[] laterEdge = laterExitEdge ? [IsTuple, ValueExit, X0] : [];
        byte[] sharedEdge = sharedReturn ? [IsTuple, CheckedReturn, X0] : [];
        byte[] outer =
        [
            Label, Four, Move, AtomTwo, X0, .. exitLine, TailExternalCallWithFrame, One, One, One,
            Label, Five, Move, Y0, X0, Line, Three, TailExternalCallWithFrame, One, One, One,
            Label, Three, Line, Four, CaseEnd, X0
        ];

        return
        [
            Label, One, FunctionInfo, AtomOne, AtomTwo, One, Label, Two,
            Allocate, One, One, Move, X0, Y0,
            IsTuple, CaseFailure, X0, IsTaggedTuple, LiteralExit, X0, Two, AtomOne,
            ExactEqualTest, ValueExit, X0, AtomTwo, .. sharedEdge,
            Line, One, ExternalCall, One, Zero, Move, X0, Y0,
            IsTaggedTuple, CheckedReturn, X0, Two, AtomOne, .. laterEdge,
            Deallocate, One, Return,
            .. (ordered ? inner : outer), .. (ordered ? outer : inner),
            Label, Seven, Line, Five, FunctionInfo, AtomOne, AtomThree, One,
            Label, Eight, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            9,
            2,
            image
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
            "RewriteInnerCheckedReturnBeforeOuterExits",
            image,
            body,
            1
        );
    }
}
