using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecordCallArgumentChecks
{
    internal const string TestName = "compiler/beam/forwards-record-update-into-outgoing-call-argument";
    private const string WrongCode = "A record update used the wrong destination for the following call.";
    private const string WrongProjection = "The heap test did not preserve the tuple until the guarded record update.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte ReuseAtom = 0x12;

    internal static Task Run()
    {
        byte[] update = [UpdateRecord, ReuseAtom, Two, Y0, X0, ExtendedListOperand, Two, Two, X0];
        byte[] direct = [UpdateRecord, ReuseAtom, Two, Y0, X1, ExtendedListOperand, Two, Two, X0];
        byte[] typedUpdate = [UpdateRecord, ReuseAtom, Two, ExtendedTypedRegisterOperand, Y0, One, X0, ExtendedListOperand, Two, Two, X0];
        byte[] typedDirect = [UpdateRecord, ReuseAtom, Two, ExtendedTypedRegisterOperand, Y0, One, X1, ExtendedListOperand, Two, Two, X0];
        byte[] typedReplacement = [UpdateRecord, ReuseAtom, Two, ExtendedTypedRegisterOperand, X1, Two, X0, ExtendedListOperand, Two, Two, X0];
        byte[] typedReplacementDirect = [UpdateRecord, ReuseAtom, Two, ExtendedTypedRegisterOperand, X1, Two, X1, ExtendedListOperand, Two, Two, X0];
        byte[] copy = [Move, X0, X1];
        byte[] overwrite = [Move, Y1, X0];
        byte[] transfer = [Line, 0x00, ExternalCall, Two, 0x00, Return, EndCode];
        byte[] original = [Label, One, .. update, .. copy, .. overwrite, .. transfer];
        byte[] expected = [Label, One, .. direct, .. overwrite, .. transfer];
        if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite([Label, One, .. typedUpdate, .. copy, .. overwrite, .. transfer]) is not { } actualTyped
            || !actualTyped.AsSpan().SequenceEqual([Label, One, .. typedDirect, .. overwrite, .. transfer])
            || Rewrite([Label, One, .. typedReplacement, .. copy, .. overwrite, .. transfer]) is not { } actualTypedAlias
            || !actualTypedAlias.AsSpan().SequenceEqual([Label, One, .. typedReplacementDirect, .. overwrite, .. transfer])
            || Rewrite([Label, One, .. update, .. copy, Move, X0, X0, .. transfer]) is not null
            || Rewrite([Label, One, .. update, .. copy, .. overwrite, ExternalCall, One, 0x00, Return, EndCode]) is not null
            || Rewrite([Label, One, .. update, .. copy, TestHeap, One, One, .. overwrite, .. transfer]) is not null)
        {
            throw new InvalidOperationException(WrongCode);
        }

        CheckUpdateProjection();

        return Task.CompletedTask;
    }

    private static void CheckUpdateProjection()
    {
        const byte failure = 0x25;
        byte[] prefix = [Label, One, Allocate, Two, Two, Move, X1, Y1, Move, X0, Y0];
        byte[] projection = [GetTupleElement, X0, One, Y0];
        byte[] guard = [IsTaggedTuple, failure, Y1, Two, ReuseAtom];
        byte[] heap = [TestHeap, Two, 0x00];
        byte[] rooted = [TestHeap, Two, One];
        byte[] update = [UpdateRecord, ReuseAtom, Two, Y1, X0, ExtendedListOperand, Two, Two, Y0];
        byte[] end = [Deallocate, Two, Return, Label, Two, BadRecord, Y1, EndCode];
        byte[] original = [.. prefix, .. projection, .. guard, .. heap, .. update, .. end];
        byte[] expected = [.. prefix, .. guard, .. rooted, .. projection, .. update, .. end];
        byte[] locatedEnd = [.. end[..^3], Line, 0x00, BadRecord, Y1, EndCode];
        if (RewriteProjection(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || RewriteProjection([.. prefix, .. projection, .. guard, .. heap, .. update, .. locatedEnd]) is not { } located
            || !located.AsSpan().SequenceEqual([.. prefix, .. guard, .. rooted, .. projection, .. update, .. locatedEnd])
            || RewriteProjection([.. prefix[..^3], .. projection, .. guard, .. heap, .. update, .. end]) is not null
            || RewriteProjection([.. prefix, .. projection, .. guard, .. heap, .. update, .. end[..^2], Y0, EndCode])
                is not null)
        {
            throw new InvalidOperationException(WrongProjection);
        }
    }

    private static byte[]? RewriteProjection(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)UpdateRecord,
            3,
            1,
            instructions
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "MoveHeapTestsBeforeNeutralInstructions",
            instructions,
            body,
            new[] { "reuse" },
            Array.Empty<(int Module, int Function, int Arity)>()
        );
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)UpdateRecord,
            2,
            1,
            instructions
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "StoreRecordUpdateInCallArgument",
            instructions,
            body
        );
    }
}
