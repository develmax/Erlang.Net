using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? SimplifyFrameInitialization(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        bool includeNonAllocatingDefinitions = false
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
            var assigned = new HashSet<int>();
            int groups = 0;
            int end = index + 1;
            for (; end < body.Count; end++)
            {
                var instruction = body[end];
                if (instruction is { Definition.Opcode: Move, Operands: [var argument, var slot] }
                    && argument.Kind == BeamOperandParameters.X && slot.Kind == BeamOperandParameters.Y)
                {
                    int register = (int)slot.Value;
                    assigned.Add(register);
                    initialized.Remove(register);
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

            bool consolidate = !includeNonAllocatingDefinitions && groups >= 2 && body[index - 1].Definition.Opcode == Label
                && body[index - 2].Definition.Opcode == FunctionInfo
                && !initialized.Overlaps(assigned);
            if (end >= body.Count)
            {
                continue;
            }

            if (!consolidate)
            {
                var edits = FindOverwrittenFrameInitializations(body, index + 1, includeNonAllocatingDefinitions);
                for (int edit = edits.Count - 1; edit >= 0; edit--)
                {
                    var (instruction, retained) = edits[edit];
                    output.Write(code.Span[copied..body[instruction].Offset]);
                    WriteFrameInitialization(output, retained);
                    copied = body[instruction + 1].Offset;
                }

                continue;
            }

            output.Write(code.Span[copied..body[index + 1].Offset]);
            WriteFrameInitialization(output, initialized);
            for (int move = index + 1; move < end; move++)
            {
                BeamInstruction instruction = body[move];
                if (instruction.Definition.Opcode == Move)
                {
                    output.Write(code.Span[instruction.Offset..body[move + 1].Offset]);
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

    private static List<(int Index, int[] Retained)> FindOverwrittenFrameInitializations(
        IReadOnlyList<BeamInstruction> body,
        int start,
        bool includeNonAllocatingDefinitions
    )
    {
        int end = start;
        bool foundDefinition = false;
        while (end + 1 < body.Count)
        {
            byte opcode = body[end].Definition.Opcode;
            if (includeNonAllocatingDefinitions && opcode is (CatchEnter or TryEnter))
            {
                // The instruction defines the protected handler slot. Leave the
                // following region untouched because its failures can branch.
                foundDefinition = true;
                end++;
                break;
            }

            if (opcode is not (Move or GetTupleElement or GetList or GetHead or GetTail
                or MakeFun or InitYRegisters or Line)
                && !(opcode == PutTuple && body[end].Operands[0].Kind == BeamOperandParameters.Y)
                && !(includeNonAllocatingDefinitions && opcode == BifZero))
            {
                break;
            }

            foundDefinition |= opcode == BifZero;
            end++;
        }

        if (includeNonAllocatingDefinitions && !foundDefinition)
        {
            return [];
        }

        var overwritten = new HashSet<int>();
        var edits = new List<(int Index, int[] Retained)>();
        for (int index = end - 1; index >= start; index--)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode == Line)
            {
                continue;
            }

            if (opcode == InitYRegisters)
            {
                int[] retained = instruction.Operands.Skip(1)
                    .Select(operand => (int)operand.Value)
                    .Where(slot => !overwritten.Contains(slot))
                    .ToArray();
                if (retained.Length < instruction.Operands.Count - 1)
                {
                    edits.Add((index, retained));
                }

                continue;
            }

            int destination = opcode is CatchEnter or TryEnter ? 0 : StackWriteOperand(instruction);
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                if (instruction.Operands[position].Kind == BeamOperandParameters.Y
                    && (position == destination || opcode == GetList && position == 2))
                {
                    overwritten.Add((int)instruction.Operands[position].Value);
                }
            }

            // Operand reads occur before the writes, including when the source aliases a destination.
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                if (instruction.Operands[position].Kind == BeamOperandParameters.Y
                    && position != destination && !(opcode == GetList && position == 2))
                {
                    overwritten.Remove((int)instruction.Operands[position].Value);
                }
            }
        }

        return edits;
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

    private byte[]? MergeAdjacentFrameInitializations(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index].Definition.Opcode != InitYRegisters || body[index + 1].Definition.Opcode != InitYRegisters)
            {
                continue;
            }

            var slots = new SortedSet<int>();
            output.Write(code.Span[copied..body[index].Offset]);
            do
            {
                foreach (BeamOperand slot in body[index].Operands.Skip(1))
                {
                    slots.Add((int)slot.Value);
                }

                index++;
            }
            while (index + 1 < body.Count && body[index].Definition.Opcode == InitYRegisters);

            // Without an intervening instruction, both clears initialize the
            // same physical frame and no observer reads the intermediate state.
            WriteFrameInitialization(output, slots);
            copied = body[index].Offset;
            index--;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
