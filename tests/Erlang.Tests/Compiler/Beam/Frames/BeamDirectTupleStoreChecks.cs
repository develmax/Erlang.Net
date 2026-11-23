using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDirectTupleStoreChecks
{
    internal const string TestName = "compiler/beam/stores-post-call-tuple-directly-when-the-result-register-dies";
    private const string Header = "BeamCodeHeader";
    private const string Reader = "BeamInstructionReader";
    private const string Rewrite = "StoreConstructionResultsDirectly";
    private const string MissingStore = "Expected the tuple to write the saved stack slot directly with the same heap roots.";
    private const string LostInput = "The tuple copy must remain when its call, overwrite, or operand proof fails.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte UnknownLine = 0x00;
    private const string SimplifyInitialization = "SimplifyFrameInitialization";
    private const byte One = 0x10;
    private const byte Three = 0x30;
    private const byte TwoTupleElements = 0x20;
    private const byte EntryLabel = 0x15;
    private const byte IntegerTwo = 0x21;

    internal static Task Run()
    {
        byte[] prefix = [LocalCall, One, EntryLabel, TestHeap, Three, One];
        byte[] tuple = [PutTuple, X0, ExtendedListOperand, TwoTupleElements, IntegerTwo, X0];
        byte[] store = [Move, X0, Y0];
        byte[] overwrite = [Move, IntegerTwo, X0, Return, EndCode];
        byte[] input = [.. prefix, .. tuple, .. store, .. overwrite];
        byte[] expected = [.. prefix, PutTuple, Y0, ExtendedListOperand, TwoTupleElements, IntegerTwo, X0, .. overwrite];
        if (Optimize(input) is not { } optimized || !optimized.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingStore);
        }

        byte[] readsResult = [.. prefix, .. tuple, .. store, Move, X0, X1, .. overwrite];
        byte[] rootsResult = [.. prefix, .. tuple, .. store, TestHeap, Three, One, .. overwrite];
        byte[] aliasesSlot = [.. prefix, PutTuple, X0, ExtendedListOperand, TwoTupleElements, Y0, X0, .. store, .. overwrite];
        byte[] noCall = [TestHeap, Three, One, .. tuple, .. store, .. overwrite];
        byte[] selfCopy = [.. prefix, .. tuple, .. store, Move, X0, X0, .. overwrite];
        byte[] joinsBlock = [.. prefix, .. tuple, .. store, Jump, EntryLabel, Label, One, .. overwrite];
        if (Optimize(readsResult) is not null || Optimize(rootsResult) is not null
            || Optimize(aliasesSlot) is not null || Optimize(noCall) is not null
            || Optimize(selfCopy) is not null || Optimize(joinsBlock) is not null)
        {
            throw new InvalidOperationException(LostInput);
        }

        byte[] entryFrame = [Line, UnknownLine, Label, One, AllocateHeap, TwoTupleElements, Three, One];
        byte[] initialize = [InitYRegisters, ExtendedListOperand, One, Y0];
        byte[] saveArgument = [Move, X0, Y1];
        byte[] directTuple = [PutTuple, Y0, ExtendedListOperand, TwoTupleElements, IntegerTwo, X0];
        byte[] loadForCall = [Move, IntegerTwo, X0];
        byte[] afterCall =
        [
            .. loadForCall, LocalCall, One, EntryLabel, Move, Y0, X0, Deallocate, TwoTupleElements, Return, EndCode
        ];
        byte[] fromEntry = [.. entryFrame, .. initialize, .. saveArgument, .. tuple, .. store, .. afterCall];
        byte[] directEntry = [.. entryFrame, .. initialize, .. saveArgument, .. directTuple, .. afterCall];
        if (Optimize(fromEntry) is not { } saved || !saved.AsSpan().SequenceEqual(directEntry))
        {
            throw new InvalidOperationException(MissingStore);
        }

        byte[] initialized = [.. entryFrame, .. saveArgument, .. directTuple, .. afterCall];
        if (Optimize(saved, SimplifyInitialization) is not { } trimmed || !trimmed.AsSpan().SequenceEqual(initialized))
        {
            throw new InvalidOperationException(MissingStore);
        }

        byte[] entryRead = [.. entryFrame, .. initialize, .. saveArgument, .. tuple, .. store, Move, X0, X1, .. afterCall];
        byte[] trimmedRoot =
        [
            .. entryFrame, InitYRegisters, ExtendedListOperand, One, Y1, Move, X0, Y0,
            .. tuple, Move, X0, Y1, .. loadForCall, TrimFrame, One, One, LocalCall, One, EntryLabel,
            Move, Y0, X0, Deallocate, One, Return, EndCode
        ];
        byte[] sendPayload =
        [
            .. entryFrame, .. initialize, .. saveArgument, .. tuple, .. store,
            .. loadForCall, Move, Y0, X1, Send, Deallocate, TwoTupleElements, Return, EndCode
        ];
        byte[] outgoingArgument =
        [
            .. entryFrame, .. initialize, .. saveArgument, .. tuple, .. store,
            .. loadForCall, Move, Y0, X1, LocalCall, TwoTupleElements, EntryLabel, Deallocate, TwoTupleElements, Return, EndCode
        ];
        byte[] interveningGc =
        [
            .. entryFrame, .. initialize, .. saveArgument, TestHeap, Three, One, .. tuple, .. store, .. afterCall
        ];
        byte[] readBeforeDefinition =
        [
            .. entryFrame,
            .. initialize,
            .. saveArgument,
            PutTuple, Y0, ExtendedListOperand, TwoTupleElements, IntegerTwo, Y0,
            .. overwrite
        ];
        if (Optimize(entryRead) is not null || Optimize(interveningGc) is not null
            || Optimize(sendPayload) is not null || Optimize(outgoingArgument) is not null
            || Optimize(trimmedRoot) is not null
            || Optimize(readBeforeDefinition, SimplifyInitialization) is not null)
        {
            throw new InvalidOperationException(LostInput);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Optimize(byte[] code, string rewrite = Rewrite)
    {
        ReadOnlyMemory<byte> instructions = code;
        object header = New(
            Header,
            0,
            (int)Swap,
            2,
            1,
            instructions
        );
        object body = Call(
            Type(Reader),
            "Read",
            header,
            1,
            0,
            null,
            null
        );

        object[] arguments = rewrite == Rewrite ? [instructions, body] : [instructions, body, false];

        return (byte[]?)Call(typeof(BeamModuleCompiler), rewrite, arguments);
    }
}
