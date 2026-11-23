using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedTupleAfterFieldEffectChecks
{
    internal const string TestName = "compiler/beam/reuses-a-checked-tuple-after-its-field-is-passed-to-an-effect";
    private const string WrongLayout = "Tuple retention changed the checked tag, field argument, or return.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte AtomOne = 0x12;
    private const byte AtomTwo = 0x22;
    private const byte AtomThree = 0x32;
    private const byte Failure = 0x35;

    internal static Task Run()
    {
        byte[] source = Image(retained: false);
        byte[] expected = Image(retained: true);
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(Image(retained: false, changedTag: true)) is not null
            || Rewrite(Image(retained: false, changedFallback: true)) is not null
            || Rewrite(expected) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(bool retained, bool changedTag = false, bool changedFallback = false)
    {
        byte[] capture = retained ? [Move, X0, Y1] : [];
        byte[] payload = retained
            ? [GetTupleElement, X0, One, X0, Move, AtomTwo, X1]
            : [GetTupleElement, X0, One, Y1, Move, AtomTwo, X1, Move, Y1, X0];
        byte[] success = retained
            ? [Move, Y1, X0, Deallocate, Two, Return]
            : [TestHeap, Three, Zero, PutTuple, X0, ExtendedListOperand, Two, changedTag ? AtomThree : AtomOne, Y1];
        byte[] fallback = changedFallback ? [Move, Y0, X0] : [];

        return
        [
            Label, One, FunctionInfo, AtomOne, AtomTwo, One, Label, Two,
            Allocate, Two, One, InitYRegisters, ExtendedListOperand, Two, Y0, Y1,
            ExternalCall, One, Zero, .. capture, IsTaggedTuple, Failure, X0, Two, AtomOne,
            .. payload, Line, One, ExternalCall, Two, One, .. success,
            Label, Three, .. fallback, Deallocate, Two, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
            1,
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
            "RewriteCheckedTupleAfterFieldEffect",
            image,
            body
        );
    }
}
