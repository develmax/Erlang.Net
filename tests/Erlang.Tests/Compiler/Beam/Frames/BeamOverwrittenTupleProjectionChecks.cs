using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOverwrittenTupleProjectionChecks
{
    internal const string TestName = "compiler/beam/removes-overwritten-tuple-projections";
    private const string MissingRewrite = "An unread X projection survived.";
    private const string UnsafeRewrite = "A tuple projection required by an input or a block boundary disappeared.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte Zero = 0x00;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Atom = 0x12;
    private const byte Nil = 0x02;
    private const byte FirstLabel = 0x15;

    internal static Task Run()
    {
        byte[] projection = [GetTupleElement, X0, One, X1];
        byte[] otherProjection = [GetTupleElement, X0, Two, X0];
        byte[] overwrite = [Move, Atom, X1];
        byte[] nextProjection = [GetTupleElement, X0, Two, X1];
        byte[] returnValue = [GetTupleElement, X1, Zero, X0];
        byte[] testHeap = [TestHeap, Two, One];
        byte[] tupleHeap = [TestHeap, Three, One];
        byte[] putList = [PutList, X0, Nil, X0];
        byte[] putTuple = [PutTuple, X0, ExtendedListOperand, Two, X0, Atom];

        CheckRemoved(
            [.. projection, .. otherProjection, .. overwrite, Return, EndCode],
            [.. otherProjection, .. overwrite, Return, EndCode]
        );
        CheckRemoved(
            [.. projection, .. nextProjection, Return, EndCode],
            [Return, EndCode]
        );
        CheckRemoved(
            [.. projection, Line, One, Deallocate, One, Return, EndCode],
            [Line, One, Deallocate, One, Return, EndCode]
        );
        CheckRemoved(
            [.. projection, .. testHeap, .. putList, Return, EndCode],
            [.. testHeap, .. putList, Return, EndCode]
        );
        CheckRemoved(
            [.. projection, .. tupleHeap, .. putTuple, Return, EndCode],
            [.. tupleHeap, .. putTuple, Return, EndCode]
        );
        CheckRemoved(
            [.. returnValue, PutTuple, X0, ExtendedListOperand, One, Atom, Return, EndCode],
            [PutTuple, X0, ExtendedListOperand, One, Atom, Return, EndCode]
        );
        if (Rewrite([.. projection, Move, X1, X2, .. overwrite, Return, EndCode]) is not null
            || Rewrite([.. projection, GetTupleElement, X1, Zero, X0, Return, EndCode]) is not null
            || Rewrite([.. projection, TestHeap, Zero, Two, .. overwrite, Return, EndCode]) is not null
            || Rewrite([.. projection, Label, One, .. overwrite, Return, EndCode]) is not null
            || Rewrite([.. returnValue, Return, EndCode]) is not null
            || Rewrite([.. returnValue, PutTuple, X0, ExtendedListOperand, One, X0, Return, EndCode]) is not null
            || Rewrite([.. projection, PutList, X1, Nil, X0, Return, EndCode]) is not null
            || Rewrite([.. projection, PutTuple, X0, ExtendedListOperand, One, X1, Return, EndCode]) is not null
            || Rewrite([.. projection, Jump, FirstLabel, Label, One, Return, EndCode]) is not null
            || Rewrite([.. projection, ExternalCall, Two, Zero, Return, EndCode]) is not null)
        {
            throw new InvalidOperationException(UnsafeRewrite);
        }

        return Task.CompletedTask;
    }

    private static void CheckRemoved(byte[] original, byte[] expected)
    {
        if (Rewrite(original) is not { } candidate || !candidate.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingRewrite);
        }
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
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
            "RemoveOverwrittenTupleProjections",
            instructions,
            body
        );
    }
}
