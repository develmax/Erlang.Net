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
        CheckListResultField(update);
        CheckClosedMapperResult();

        return Task.CompletedTask;
    }

    private static void CheckClosedMapperResult()
    {
        var compiler = new BeamModuleCompiler("closed_mapper_record_probe");
        var mapper = (FunctionDefinition)Call(
            typeof(BeamModuleCompiler),
            "BuildClosedMapping",
            "closed",
            new Pattern.Variable("Element"),
            new Expr.Variable("Element"),
            null
        );
        var entries = new Dictionary<(string Name, int Arity), int> { [("closed", 1)] = 2, [("open", 1)] = 4 };
        var results = (HashSet<int>)Call(
            compiler,
            "ReadNilTerminatedMapperEntries",
            new FunctionDefinition[] { mapper, mapper with { Name = "open", HasNilTerminatedMapperResult = false } },
            entries
        );
        if (!results.SetEquals([2]))
        {
            throw new InvalidOperationException(WrongProjection);
        }
    }

    private static void CheckListResultField(byte[] update)
    {
        var compiler = new BeamModuleCompiler("list_record_operand_probe");
        _ = Call(compiler, "Intern", "reuse");
        const int callee = 2;
        int listType = (int)typeof(BeamModuleCompiler)
            .GetField("ProperListTypeBits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetRawConstantValue()!;
        var resultTypes = new Dictionary<int, int> { [callee] = listType };
        const byte target = 0x25;
        byte[] typed = [.. update[..^1], ExtendedTypedRegisterOperand, X0, One];
        byte[] original = [Label, One, LocalCall, One, target, TestHeap, Two, One, .. update, Return, Label, Two, Return, EndCode];
        byte[] expected = [Label, One, LocalCall, One, target, TestHeap, Two, One, .. typed, Return, Label, Two, Return, EndCode];
        byte[] overwritten =
        [
            Label, One, LocalCall, One, target, TestHeap, Two, One, Move, Y1, X0,
            .. update, Return, Label, Two, Return, EndCode
        ];
        if (Annotate(original) is not { } result || !result.AsSpan().SequenceEqual(expected)
            || Annotate(overwritten) is not null || Annotate(result) is not null
            || Annotate(original, recordOnly: true) is not { } fromMapper || !fromMapper.AsSpan().SequenceEqual(expected)
            || Annotate([Label, One, LocalCall, One, target, IsNil, target, X0, Return, Label, Two, Return, EndCode],
                recordOnly: true) is not null)
        {
            throw new InvalidOperationException(WrongProjection);
        }

        byte[]? Annotate(byte[] code, bool recordOnly = false) => (byte[]?)Call(
            compiler,
            "AnnotateListProducerResults",
            new ReadOnlyMemory<byte>(code),
            3,
            recordOnly ? new Dictionary<int, int>() : resultTypes,
            null,
            recordOnly ? new HashSet<int> { callee } : null
        );
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
