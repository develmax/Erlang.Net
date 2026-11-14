using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CombineStackAndHeapAllocation(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        MemoryStream? output = null;
        int copied = 0;
        for (int start = 0; start + 2 < body.Count; start++)
        {
            if (body[start] is not { Definition.Opcode: Allocate, Operands: [var frame, var live] } allocation)
            {
                continue;
            }

            int end = start + 1;
            while (body[end].Definition.Opcode is Move or InitYRegisters)
            {
                end++;
            }

            if (end + 1 >= body.Count
                || body[end] is not { Definition.Opcode: TestHeap, Operands: [var heap, _] } heapCheck
                || heap.Kind != BeamOperandParameters.Unsigned || heap.Value.Sign <= 0)
            {
                continue;
            }

            output ??= new MemoryStream(code.Length);
            output.Write(code.Span[copied..allocation.Offset]);
            output.WriteByte(AllocateHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, heap.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
            output.Write(code.Span[body[start + 1].Offset..heapCheck.Offset]);
            copied = body[end + 1].Offset;
            start = end;
        }

        if (output is null)
        {
            return null;
        }

        using (output)
        {
            output.Write(code.Span[copied..]);

            return output.ToArray();
        }
    }
}
