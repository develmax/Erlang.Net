using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedCatchArgumentChecks
{
    internal const string TestName = "compiler/beam/places-checked-tuple-call-arguments-before-the-catch-frame";
    private const string Incorrect = "The early checked fields must preserve the argument exchange, call line, and catch path.";
    private const string Unsafe = "A catch-result path observing a saved field must retain its original frame.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte Tag = 0x32;
    private const byte Failure = 0x15;
    private const byte Landing = 0x35;
    private const byte Callee = 0x45;

    internal static Task Run()
    {
        byte[] entry =
        [
            Label, One, FunctionInfo, Module, Function, One, Label, Two,
            IsTaggedTuple, Failure, X0, Three, Tag
        ];
        byte[] layout =
        [
            Allocate, Three, One,
            GetTupleElement, X0, One, Y1,
            GetTupleElement, X0, Two, Y0,
            CatchEnter, Y2, Landing,
            Move, Y0, X1,
            Move, Y1, X0
        ];
        byte[] call = [Line, One, LocalCall, Two, Callee, Label, Three];
        byte[] close = [CatchEnd, Y2];
        byte[] source = [.. entry, .. layout, .. call, .. close, Deallocate, Three, Return, EndCode];
        byte[] expected =
        [
            .. entry, GetTupleElement, X0, One, X1, GetTupleElement, X0, Two, X0,
            Allocate, One, Two, CatchEnter, Y0, Landing, Swap, X0, X1,
            .. call, CatchEnd, Y0, Deallocate, One, Return, EndCode
        ];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(Incorrect);
        }

        byte[] usesFieldAfterCatch =
        [
            .. entry, .. layout, .. call, .. close,
            Move, Y0, X0, Deallocate, Three, Return, EndCode
        ];
        if (Rewrite(usesFieldAfterCatch) is not null)
        {
            throw new InvalidOperationException(Unsafe);
        }

        CheckTry(entry);

        return Task.CompletedTask;
    }

    private static void CheckTry(byte[] entry)
    {
        byte[] prefix =
        [
            .. entry, Allocate, Three, One,
            GetTupleElement, X0, One, Y1,
            GetTupleElement, X0, Two, Y0,
            TryEnter, Y2, Landing,
            Move, Y0, X1,
            Move, Y1, X0,
            Line, One, LocalCall, Two, Callee, TryEnd, Y2
        ];
        byte[] finish = [Deallocate, Three, Return];
        byte[] exceptional = [Label, Three, TryCase, Y2];
        byte[] split = [.. prefix, .. finish, .. exceptional, .. finish, EndCode];
        byte[] expected =
        [
            .. entry, GetTupleElement, X0, One, X1, GetTupleElement, X0, Two, X0,
            Allocate, One, Two, TryEnter, Y0, Landing, Swap, X0, X1,
            Line, One, LocalCall, Two, Callee, TryEnd, Y0, Deallocate, One, Return,
            Label, Three, TryCase, Y0, Deallocate, One, Return, EndCode
        ];
        if (Rewrite(split) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(Incorrect);
        }

        byte[] live = [.. prefix, .. finish, .. exceptional, Move, Y0, X0, .. finish, EndCode];
        byte[] competing = [.. prefix, Jump, Landing, .. exceptional, .. finish, EndCode];
        if (Rewrite(live) is not null || Rewrite(competing) is not null)
        {
            throw new InvalidOperationException(Unsafe);
        }
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)Swap,
            5,
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
            "RewriteCheckedCatchArguments",
            instructions,
            body
        );
    }
}
