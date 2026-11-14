using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? KeepTerminalCallResultsInX0(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int stackIndex = 0;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        for (int index = 0; index + 6 < body.Count; index++)
        {
            if (body[index].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index + 1] is not { Definition.Opcode: Move, Operands: [var source, var saved] }
                || source != result || saved.Kind != BeamOperandParameters.Y
                || body[index + 2] is not { Definition.Opcode: TestHeap, Operands: [_, var live] } heap
                || !live.Value.IsZero || body[index + 4].Definition.Opcode != Deallocate
                || body[index + 5].Definition.Opcode != Return)
            {
                continue;
            }

            BeamInstruction construction = body[index + 3];
            bool returnsSavedResult = construction.Definition.Opcode switch
            {
                PutTuple => construction.Operands[0] == result
                    && construction.Operands.Skip(2).Any(operand => SameProjectionRegister(operand, saved)),
                PutList => construction.Operands[2] == result
                    && (SameProjectionRegister(construction.Operands[0], saved)
                        || SameProjectionRegister(construction.Operands[1], saved)),
                _ => false
            };
            if (!returnsSavedResult)
            {
                continue;
            }

            output.Write(code.Span[copied..body[index + 1].Offset]);
            // The final compact operand of test_heap is the zero live-register count.
            output.Write(code.Span[heap.Offset..(construction.Offset - 1)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            copied = construction.Offset;
            while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < construction.Offset)
            {
                stackIndex++;
            }

            int end = body[index + 4].Offset;
            while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < end)
            {
                var (start, finish, slot) = stackOperands[stackIndex++];
                if (slot == saved.Value)
                {
                    output.Write(code.Span[copied..start]);
                    WriteRegister(output, result);
                    copied = finish;
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
