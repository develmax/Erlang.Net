using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamReturnedTupleCaptureChecks
{
    internal const string TestName = "compiler/beam/captures-a-returned-tuple-before-its-forward-check";
    private const string WrongLayout = "The call result, its checked path, or its stack arguments were scheduled incorrectly.";
    private const byte X0 = 0x03;
    private const byte Y0 = 0x04;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte AtomOne = 0x12;
    private const byte AtomTwo = 0x22;
    private const byte Failure = 0x35;

    internal static Task Run()
    {
        byte[] source = Image(hoisted: false, readOnFailure: false);
        byte[] expected = Image(hoisted: true, readOnFailure: false);
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(Image(hoisted: false, readOnFailure: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(bool hoisted, bool readOnFailure)
    {
        byte[] test = [IsTaggedTuple, Failure, X0, Two, AtomTwo];
        byte[] capture = [Move, X0, Y0];
        byte[] fallback = readOnFailure ? [Move, Y0, X0] : [];

        return
        [
            Label, One, FunctionInfo, AtomOne, AtomTwo, One, Label, Two,
            Allocate, One, One, InitYRegisters, ExtendedListOperand, One, Y0,
            ExternalCall, One, Zero,
            .. (hoisted ? capture : test), .. (hoisted ? test : capture),
            GetTupleElement, X0, One, X0, Deallocate, One, Return,
            Label, Three, .. fallback, Deallocate, One, Return, EndCode
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
            3,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteReturnedTupleCapture",
            image,
            body
        );
    }
}
