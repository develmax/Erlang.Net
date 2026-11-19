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

        return Task.CompletedTask;
    }

    private static byte[]? Optimize(byte[] code)
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

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            Rewrite,
            instructions,
            body
        );
    }
}
