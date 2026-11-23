using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTailArgumentCopyChecks
{
    internal const string TestName = "compiler/beam/composes-redundant-tail-argument-copies";
    private const string MissingComposition = "The terminal argument permutation should need fewer moves and preserve the literal and source line.";
    private const string UnexpectedComposition = "A minimal sequence, cycle, or frame with effects, another exit, or undefined values must not be composed.";
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Nine = 0x90;
    private const byte Ten = 0xa0;
    private const byte Eleven = 0xb0;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte Target = 0x35;
    private const byte Literal = 0x47;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte X5 = 0x53;
    private const byte X6 = 0x63;
    private const byte X7 = 0x73;
    private const byte X8 = 0x83;
    private const byte X9 = 0x93;
    private const byte X10 = 0xa3;
    private const byte X11 = 0xb3;
    private const byte X12 = 0xc3;

    internal static Task Run()
    {
        // The outgoing operands from the Snmp Inform v2/v3 default-extra-info wrappers.
        byte[] v2 =
        [
            Move, X6, X9, Move, X7, X10, Move, X8, X11, Move, X5, X6,
            Move, Literal, Zero, X5, Move, X9, X7, Move, X10, X8, Move, X11, X9
        ];
        byte[] v2Expected =
        [
            Move, X8, X9, Move, X7, X8, Move, X6, X7, Move, X5, X6, Move, Literal, Zero, X5
        ];
        byte[] v3 =
        [
            Move, X7, X10, Move, X8, X11, Move, X9, X12, Move, X6, X7,
            Move, Literal, Zero, X6, Move, X10, X8, Move, X11, X9, Move, X12, X10
        ];
        byte[] v3Expected =
        [
            Move, X9, X10, Move, X8, X9, Move, X7, X8, Move, X6, X7, Move, Literal, Zero, X6
        ];
        byte[] directV2 = [.. Entry(Nine), .. v2, Line, One, TailLocalCall, Ten, Target, EndCode];
        byte[] expectedV2 = [.. Entry(Nine), .. v2Expected, Line, One, TailLocalCall, Ten, Target, EndCode];
        byte[] directV3 = [.. Entry(Ten), .. v3, TailLocalCall, Eleven, Target, EndCode];
        byte[] expectedV3 = [.. Entry(Ten), .. v3Expected, TailLocalCall, Eleven, Target, EndCode];
        if (Rewrite(directV2) is not { } first || !first.AsSpan().SequenceEqual(expectedV2)
            || Rewrite(directV3) is not { } second || !second.AsSpan().SequenceEqual(expectedV3))
        {
            throw new InvalidOperationException(MissingComposition);
        }

        byte[] minimal = [.. Entry(Two), Move, X1, X2, Move, X0, X1, TailLocalCall, Three, Target, EndCode];
        byte[] cycle =
        [
            .. Entry(Two), Move, X0, X2, Move, X1, X0, Move, X2, X1, TailLocalCall, Two, Target, EndCode
        ];
        byte[] line = [.. Entry(One), Move, X0, X2, Line, One, Move, X2, X1, TailLocalCall, Two, Target, EndCode];
        byte[] label = [.. Entry(One), Move, X0, X2, Label, Three, Move, X2, X1, TailLocalCall, Two, Target, EndCode];
        byte[] framed = [.. Entry(Nine), .. v2, TailLocalCallWithFrame, Ten, Target, One, EndCode];
        if (Rewrite(minimal) is not null || Rewrite(cycle) is not null || Rewrite(line) is not null
            || Rewrite(label) is not null || Rewrite(framed) is not null)
        {
            throw new InvalidOperationException(UnexpectedComposition);
        }

        CheckMoveOnlyFrame();
        CheckGuardedProjectionArguments();

        return Task.CompletedTask;
    }

    private static void CheckMoveOnlyFrame()
    {
        const byte Four = 0x40;
        const byte Five = 0x50;
        const byte X3 = 0x33;
        const byte Y0 = 0x04;
        const byte FunctionFailure = 0x15;
        const byte Binary = 0x35;
        const byte Joined = 0x45;
        const byte Callee = 0x55;
        const byte Argument = 0x31;
        byte[] guard =
        [
            IsPid, FunctionFailure, X0,
            IsList, Binary, X1,
            Jump, Joined,
            Label, Three, IsBinary, FunctionFailure, X1,
            Label, Four, IsList, FunctionFailure, X2
        ];
        byte[] copies =
        [
            Move, X2, Y0, Move, X1, X2, Move, X0, X1, Move, Y0, X3, Move, Argument, X0
        ];
        byte[] framed = [.. Entry(Three), .. guard, Allocate, One, Three, .. copies, Line, One, TailLocalCallWithFrame, Four, Callee, One, EndCode];
        byte[] expected =
        [
            .. Entry(Three), .. guard,
            Move, X2, X3, Move, X1, X2, Move, X0, X1, Move, Argument, X0,
            Line, One, TailLocalCall, Four, Callee, EndCode
        ];
        if (Rewrite(framed) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingComposition);
        }

        byte[] saveIdentity =
        [
            .. Entry(Two), Allocate, One, Two,
            Move, X0, Y0, Move, Literal, Zero, X2, Move, Y0, X0,
            TailLocalCallWithFrame, Three, Callee, One, EndCode
        ];
        byte[] identityExpected = [.. Entry(Two), Move, Literal, Zero, X2, TailLocalCall, Three, Callee, EndCode];
        if (Rewrite(saveIdentity) is not { } identity || !identity.AsSpan().SequenceEqual(identityExpected))
        {
            throw new InvalidOperationException(MissingComposition);
        }

        byte[] shortLive = [.. Entry(Three), .. guard, Allocate, One, Two, .. copies, TailLocalCallWithFrame, Four, Callee, One, EndCode];
        byte[] callBeforeTail =
        [
            .. Entry(Three), .. guard, Allocate, One, Three, Move, X2, Y0,
            ExternalCall, One, Zero, Move, Y0, X3, TailLocalCallWithFrame, Four, Callee, One, EndCode
        ];
        byte[] uninitialized =
        [
            .. Entry(Three), .. guard, Allocate, One, Three, Move, Y0, X3,
            Move, X1, X2, TailLocalCallWithFrame, Four, Callee, One, EndCode
        ];
        byte[] external =
        [
            .. Entry(Three), .. guard, Allocate, One, Three, .. copies,
            TailExternalCallWithFrame, Four, Zero, One, EndCode
        ];
        byte[] otherGuardExit =
        [
            .. Entry(Three), IsList, Callee, X1, Allocate, One, Three, .. copies,
            TailLocalCallWithFrame, Four, Callee, One,
            Label, Five, Return, EndCode
        ];
        if (Rewrite(shortLive) is not null || Rewrite(callBeforeTail) is not null
            || Rewrite(uninitialized) is not null || Rewrite(external) is not null || Rewrite(otherGuardExit) is not null)
        {
            throw new InvalidOperationException(UnexpectedComposition);
        }
    }

    private static void CheckGuardedProjectionArguments()
    {
        const byte X3 = 0x33;
        const byte Failure = 0x35;
        const byte OtherFailure = 0x45;
        byte[] shape =
        [
            IsTuple, Failure, X0, TestArity, Failure, X0, Two,
            GetTupleElement, X0, Zero, X1, IsPid, Failure, X1
        ];
        byte[] expectedShape =
        [
            IsTuple, Failure, X0, TestArity, Failure, X0, Two,
            GetTupleElement, X0, Zero, X3, IsPid, Failure, X3
        ];
        byte[] success = [TailLocalCall, Three, Target];
        byte[] failure = [Label, Three, TailLocalCall, One, Target, EndCode];
        byte[] saved = [.. Entry(Three), Move, X1, X3, .. shape, Move, X3, X0];
        byte[] candidate = [.. saved, .. success, .. failure];
        byte[] expected =
        [
            .. Entry(Three), .. expectedShape, Move, X1, X0, Move, X3, X1, .. success, .. failure
        ];
        if (Rewrite(candidate) is not { } result || !result.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingComposition);
        }

        byte[] failureReadsParameter =
        [
            .. saved, .. success, Label, Three, TailLocalCall, Two, Target, EndCode
        ];
        byte[] separatePredicateFailure =
        [
            .. Entry(Three), Move, X1, X3, .. shape[..^3], IsPid, OtherFailure, X1,
            Move, X3, X0, .. success, .. failure
        ];
        if (Rewrite(failureReadsParameter) is not null || Rewrite(separatePredicateFailure) is not null)
        {
            throw new InvalidOperationException(UnexpectedComposition);
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
            3,
            1,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "ComposeTailArgumentCopies",
            instructions,
            body
        );
    }

    private static byte[] Entry(byte arity) => [Label, One, FunctionInfo, Module, Function, arity, Label, Two];
}
