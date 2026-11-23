using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ReadOnlyMemory<byte> OrderTypeOperandsByFirstUse(
        ReadOnlyMemory<byte> code,
        int labelCount,
        bool discardUnused = false
    )
    {
        // OTP seeds Any at index zero, so one additional type cannot move.
        if (typeBits.Count <= (discardUnused ? 1 : 2))
        {
            return code;
        }

        var indices = new int[typeBits.Count];
        Array.Fill(indices, -1);
        indices[0] = 0;
        var order = new List<int>(typeBits.Count) { 0 };
        var edits = new List<(int Start, int End, int Index)>();
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            var body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
            foreach (BeamInstruction instruction in body)
            {
                if (!instruction.Operands.Any(operand => operand.TypeIndex.HasValue))
                {
                    continue;
                }

                int start = instruction.Offset + 1;
                var cursor = new BeamCursor(code[start..]);
                foreach (BeamOperand operand in instruction.Operands)
                {
                    var wire = BeamCompactReader.Read(cursor);
                    if (wire.WireTag != BeamOperandParameters.ExtendedTag)
                    {
                        continue;
                    }

                    switch ((int)wire.Value)
                    {
                        case BeamOperandParameters.ArgumentList:
                        case BeamOperandParameters.Literal:
                        case BeamOperandParameters.FloatRegister:
                            // The list's elements are subsequent decoded operands.
                            _ = BeamCompactReader.Read(cursor);
                            break;
                        case BeamOperandParameters.AllocationList:
                            int count = (int)BeamCompactReader.Read(cursor).Value;
                            for (int index = 0; index < count; index++)
                            {
                                _ = BeamCompactReader.Read(cursor);
                                _ = BeamCompactReader.Read(cursor);
                            }

                            break;
                        case BeamOperandParameters.TypedRegister:
                            _ = BeamCompactReader.Read(cursor);
                            int offset = start + cursor.Position;
                            int oldIndex = (int)BeamCompactReader.Read(cursor).Value;
                            if (oldIndex >= typeBits.Count || oldIndex != operand.TypeIndex)
                            {
                                return code;
                            }

                            if (indices[oldIndex] < 0)
                            {
                                indices[oldIndex] = order.Count;
                                order.Add(oldIndex);
                            }

                            int replacement = indices[oldIndex];
                            if (replacement != oldIndex)
                            {
                                edits.Add((offset, start + cursor.Position, replacement));
                            }

                            break;
                        default:
                            return code;
                    }
                }
            }
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return code;
        }

        bool hasUnused = order.Count != typeBits.Count;
        if (hasUnused && !discardUnused || !hasUnused && edits.Count == 0)
        {
            return code;
        }

        // Only final assembly owns removal: earlier passes can still emit new
        // references with the existing type indices.
        var rows = order.Select(index => typeBits[index]).ToArray();
        typeBits.Clear();
        typeBits.AddRange(rows);
        if (edits.Count == 0)
        {
            return code;
        }

        using var output = new MemoryStream();
        int copied = 0;
        foreach (var (start, end, index) in edits)
        {
            output.Write(code.Span[copied..start]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, index);
            copied = end;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
