using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ConsolidateEntryInitialization(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 2; index + 2 < body.Count; index++)
        {
            if (body[index].Definition.Opcode is not (Allocate or AllocateHeap)
                || body[index - 1].Definition.Opcode != Label
                || body[index - 2].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            // Incoming X-to-Y copies neither read the frame nor trigger garbage collection.
            var initialized = new SortedSet<int>();
            var assigned = new HashSet<int>();
            int groups = 0;
            int end = index + 1;
            for (; end < body.Count; end++)
            {
                var instruction = body[end];
                if (instruction is { Definition.Opcode: Move, Operands: [var argument, var slot] }
                    && argument.Kind == BeamOperandParameters.X && slot.Kind == BeamOperandParameters.Y)
                {
                    assigned.Add((int)slot.Value);
                    initialized.Remove((int)slot.Value);
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

            if (groups < 2 || initialized.Overlaps(assigned) || end >= body.Count)
            {
                continue;
            }

            output.Write(code.Span[copied..body[index + 1].Offset]);
            if (initialized.Count != 0)
            {
                output.WriteByte(InitYRegisters);
                output.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, initialized.Count);
                foreach (int slot in initialized)
                {
                    BeamCompactWriter.Write(output, YRegisterTag, slot);
                }
            }

            for (int move = index + 1; move < end; move++)
            {
                if (body[move].Definition.Opcode == Move)
                {
                    output.Write(code.Span[body[move].Offset..body[move + 1].Offset]);
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
}
