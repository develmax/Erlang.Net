using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? MoveHeapTestsBeforeTupleReads(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 1; index + 1 < body.Count; index++)
        {
            BeamInstruction heap = body[index];
            if (heap.Definition.Opcode != TestHeap || heap.Operands[^1].Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int original = (int)heap.Operands[^1].Value;
            int live = original;
            int first = index;
            while (first > 0 && body[first - 1] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                        _,
                        { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                } && source.Value < live)
            {
                if (destination.Value == live - 1 && source.Value < destination.Value)
                {
                    live--;
                }
                else if (destination.Value < live && destination.Value != source.Value)
                {
                    // Dropping an interior register would leave an unproven slot in the GC root prefix.
                    break;
                }

                first--;
            }

            if (live == original)
            {
                continue;
            }

            int start = body[first].Offset;
            int end = body[index + 1].Offset;
            int width = original < 1 << BeamWire.CompactSmallPayloadShift ? 1 : 2;
            output.Write(code.Span[copied..start]);
            output.Write(code.Span[heap.Offset..(end - width)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
            output.Write(code.Span[start..heap.Offset]);
            copied = end;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        return output.ToArray();
    }
}
