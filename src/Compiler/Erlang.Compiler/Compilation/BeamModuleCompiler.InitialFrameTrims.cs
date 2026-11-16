using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FoldInitialFrameTrims(ReadOnlyMemory<byte> code, int labelCount)
    {
        var stackOperands = new List<(int Start, int End, int Slot)>();
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                recordStackOperand: (start, end, slot) => stackOperands.Add((start, end, slot))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction allocation = body[index];
            if (allocation.Definition.Opcode is not (Allocate or AllocateHeap)
                || FindInitialFrameTrim(body, index) is not { } trim)
            {
                continue;
            }

            int removed = (int)body[trim].Operands[0].Value;
            int kept = (int)body[trim].Operands[1].Value;
            int sizeStart = allocation.Offset + 1;
            var cursor = new BeamCursor(code[sizeStart..]);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[copied..sizeStart]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, kept);
            copied = sizeStart + cursor.Position;
            for (int prefix = index + 1; prefix < trim; prefix++)
            {
                BeamInstruction instruction = body[prefix];
                int next = body[prefix + 1].Offset;
                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    int[] slots = instruction.Operands.Skip(1).Where(slot => slot.Value >= removed)
                        .Select(slot => (int)slot.Value - removed).ToArray();
                    output.Write(code.Span[copied..instruction.Offset]);
                    if (slots.Length != 0)
                    {
                        WriteFrameInitialization(output, slots);
                    }

                    copied = next;
                    continue;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                {
                    operand++;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                {
                    var (from, to, slot) = stackOperands[operand++];
                    output.Write(code.Span[copied..from]);
                    BeamCompactWriter.Write(output, YRegisterTag, slot - removed);
                    copied = to;
                }
            }

            output.Write(code.Span[copied..body[trim].Offset]);
            copied = body[trim + 1].Offset;
            index = trim;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int? FindInitialFrameTrim(IReadOnlyList<BeamInstruction> body, int allocation)
    {
        int size = (int)body[allocation].Operands[0].Value;
        int firstUsed = size;
        for (int index = allocation + 1; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode == TrimFrame)
            {
                int removed = (int)instruction.Operands[0].Value;
                int kept = (int)instruction.Operands[1].Value;

                return removed > 0 && removed <= firstUsed && removed + kept == size ? index : null;
            }

            if (opcode == InitYRegisters)
            {
                continue;
            }

            // Once the frame can be observed along another path, the smaller frame might not fit that path.
            if (opcode is not (Line or Move or Swap or GetTupleElement or GetList or GetHead or GetTail
                or PutTuple or PutList or TestHeap))
            {
                break;
            }

            foreach (BeamOperand operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y)
                {
                    firstUsed = Math.Min(firstUsed, (int)operand.Value);
                }
            }
        }

        return null;
    }
}
