using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CombineTerminalProjectedListHeap(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        const int taggedPairHeapWords = 3;
        const int tailHeapWords = ConsHeapWords + taggedPairHeapWords;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var body = decoded.Body;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 9 < body.Count; index++)
        {
            if (body[index] is not
                { Definition.Opcode: TestHeap, Operands: [{ Value: var firstHeap }, { Value.IsOne: true }] }
                || firstHeap != taggedPairHeapWords
                || !IsTaggedPair(body[index + 1])
                || body[index + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var headSource, { Kind: BeamOperandParameters.Y, TypeIndex: null } temporary]
                }
                || headSource != x0
                || body[index + 3] is not
                { Definition.Opcode: TestHeap, Operands: [{ Value: var secondHeap }, { Value.IsZero: true }] }
                || secondHeap != tailHeapWords
                || body[index + 4] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } tuple, _, var tail]
                }
                || tuple == temporary || tail != x0
                || body[index + 5] is not
                { Definition.Opcode: PutList, Operands: [var head, var listTail, var list] }
                || head != temporary || listTail != x0 || list != x0
                || !IsTaggedPair(body[index + 6])
                || body[index + 7].Definition.Opcode != Deallocate
                || body[index + 8].Definition.Opcode != Return)
            {
                continue;
            }

            // The record stays rooted in Y across the reservation. The call result
            // stays in X0 and the projected tail takes X1 after the only heap check.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, taggedPairHeapWords + tailHeapWords);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.Write(code.Span[body[index + 4].Offset..(body[index + 5].Offset - 1)]);
            WriteRegister(output, x1);
            output.Write(code.Span[body[index + 1].Offset..body[index + 2].Offset]);
            output.WriteByte(PutList);
            WriteRegister(output, x0);
            WriteRegister(output, x1);
            WriteRegister(output, x0);
            copied = body[index + 6].Offset;
            index += 8;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsTaggedPair(BeamInstruction instruction) =>
            instruction is
            {
                Definition.Opcode: PutTuple,
                Operands: [var destination, _, { Kind: BeamOperandParameters.Atom }, var payload]
            } && destination == x0 && payload == x0;
    }
}
