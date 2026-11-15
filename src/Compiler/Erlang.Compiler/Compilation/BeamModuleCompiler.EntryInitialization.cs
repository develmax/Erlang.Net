using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? SimplifyFrameInitialization(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 2; index + 2 < body.Count; index++)
        {
            if (body[index].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                continue;
            }

            // Incoming X-to-Y copies neither read the frame nor trigger garbage collection.
            var initialized = new SortedSet<int>();
            var lastAssignment = new Dictionary<int, int>();
            bool overwritten = false;
            int groups = 0;
            int end = index + 1;
            for (; end < body.Count; end++)
            {
                var instruction = body[end];
                if (instruction is { Definition.Opcode: Move, Operands: [var argument, var slot] }
                    && argument.Kind == BeamOperandParameters.X && slot.Kind == BeamOperandParameters.Y)
                {
                    int register = (int)slot.Value;
                    lastAssignment[register] = end;
                    overwritten |= initialized.Remove(register);
                }
                else if (instruction.Definition.Opcode == InitYRegisters)
                {
                    groups++;
                    foreach (var register in instruction.Operands.Skip(1))
                    {
                        initialized.Add((int)register.Value);
                    }
                }
                else
                {
                    break;
                }
            }

            bool consolidate = groups >= 2 && body[index - 1].Definition.Opcode == Label
                && body[index - 2].Definition.Opcode == FunctionInfo
                && !initialized.Any(lastAssignment.ContainsKey);
            if ((!consolidate && !overwritten) || end >= body.Count)
            {
                continue;
            }

            output.Write(code.Span[copied..body[index + 1].Offset]);
            if (consolidate)
            {
                WriteFrameInitialization(output, initialized);
            }

            for (int move = index + 1; move < end; move++)
            {
                BeamInstruction instruction = body[move];
                if (instruction.Definition.Opcode == Move)
                {
                    output.Write(code.Span[instruction.Offset..body[move + 1].Offset]);
                }
                else if (!consolidate)
                {
                    // Keep a clear that follows the slot's last assignment in this prefix.
                    int[] retained = instruction.Operands.Skip(1)
                        .Select(slot => (int)slot.Value)
                        .Where(slot => lastAssignment.GetValueOrDefault(slot, -1) < move)
                        .ToArray();
                    WriteFrameInitialization(output, retained);
                }
            }

            copied = body[end].Offset;
            index = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static void WriteFrameInitialization(Stream output, IReadOnlyCollection<int> slots)
    {
        if (slots.Count == 0)
        {
            return;
        }

        output.WriteByte(InitYRegisters);
        output.WriteByte(ExtendedListOperand);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, slots.Count);
        foreach (int slot in slots)
        {
            BeamCompactWriter.Write(output, YRegisterTag, slot);
        }
    }
}
