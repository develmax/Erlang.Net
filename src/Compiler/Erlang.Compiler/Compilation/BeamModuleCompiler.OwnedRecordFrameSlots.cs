using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderOwnedRecordCallFrameSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(UpdateRecord))
        {
            return null;
        }

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
                recordStackOperand: (from, to, slot) => stackOperands.Add((from, to, slot))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        for (int info = 0; info + 6 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int entry = info + 1;
            int end = entry;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            if (PlanOwnedRecordCallSlots(body, entry, end) is not { } plan)
            {
                continue;
            }

            int first = entry + 2;
            output.Write(code.Span[copied..body[first].Offset]);
            WriteFrameInitialization(output, [0]);
            foreach (var save in plan.Saves.OrderBy(save => plan.Slots[save.Slot]))
            {
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, XRegisterTag, save.Argument);
                BeamCompactWriter.Write(output, YRegisterTag, plan.Slots[save.Slot]);
            }

            copied = body[first + plan.Saves.Length + 1].Offset;
            for (int index = first + plan.Saves.Length + 1; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                int next = body[index + 1].Offset;
                while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                {
                    operand++;
                }

                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    int[] slots = instruction.Operands.Skip(1).Select(register => plan.Slots[(int)register.Value])
                        .Order().ToArray();
                    WriteFrameInitialization(output, slots);
                    copied = next;
                    continue;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                {
                    var (from, to, slot) = stackOperands[operand++];
                    output.Write(code.Span[copied..from]);
                    BeamCompactWriter.Write(output, YRegisterTag, plan.Slots[slot]);
                    copied = to;
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

    private static (int[] Slots, (int Argument, int Slot)[] Saves)? PlanOwnedRecordCallSlots(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end
    )
    {
        if (entry + 4 >= end || body[entry].Definition.Opcode != Label
            || body[entry + 1] is not { Definition.Opcode: Allocate, Operands: [var count, var live] }
            || count.Value < 2 || count.Value > BeamOperandParameters.MaximumRegister
            || body[entry + 2] is not
            {
                Definition.Opcode: InitYRegisters,
                Operands: [_, { Kind: BeamOperandParameters.Y } pending]
            }
            || pending.Value >= count.Value)
        {
            return null;
        }

        int width = (int)count.Value;
        var saved = new List<(int Argument, int Slot)>(width - 1);
        int after = entry + 3;
        for (; after < end && body[after] is
        {
            Definition.Opcode: Move,
            Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } input,
            { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
        }; after++)
        {
            if (slot.Value >= width || input.Value >= live.Value || slot.Value == pending.Value
                || saved.Any(save => save.Slot == slot.Value || save.Argument == input.Value))
            {
                return null;
            }

            saved.Add(((int)input.Value, (int)slot.Value));
        }

        if (saved.Count != width - 1)
        {
            return null;
        }

        // In pre-codegen the logical Y intervals for the copied arguments
        // begin in source argument order. A later overlapping definition
        // gets the next slot, and turn_yregs reverses the physical frame.
        int[] slots = new int[width];
        int target = width - 1;
        foreach (var save in saved.OrderBy(save => save.Argument))
        {
            slots[save.Slot] = target--;
        }

        if (slots.Where((slot, index) => slot != index).Any() == false)
        {
            return null;
        }

        int projection = ReadSingleFrameField(
            body,
            after,
            end,
            pending
        );
        if (projection < 0)
        {
            return null;
        }

        int update = projection + 1;
        while (update < end && body[update].Definition.Opcode is Line or TestHeap)
        {
            update++;
        }

        if (update >= end || body[update].Definition.Opcode != UpdateRecord)
        {
            return null;
        }

        int call = update + 1;
        while (call < end && body[call].Definition.Opcode is Move or InitYRegisters or Line)
        {
            call++;
        }

        if (call >= end || ReadRecordCallSlotUses(body, entry, call) is not { } setup || setup.Start != update
            || !Enumerable.Range(0, width).All(slot => setup.Reads.Any(read => read.Value == slot))
            || ReadDisjointFrameOwnership(body, entry, end) is not { } ownership
            || ownership.Owners[projection - entry] != entry + 1 || ownership.Owners[call - entry] != entry + 1)
        {
            return null;
        }

        return (slots, saved.ToArray());
    }

    private static int ReadSingleFrameField(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        BeamOperand pending
    )
    {
        int projection = -1;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Allocate or AllocateHeap or TrimFrame or TryEnter or CatchEnter or DebugLine or ExecutableLine)
            {
                return -1;
            }

            if (opcode == InitYRegisters)
            {
                if (projection < 0)
                {
                    return -1;
                }

                continue;
            }

            if (!instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y)
                || IsEntryFramePredicate(opcode) || opcode is SelectValue or SelectTupleArity)
            {
                continue;
            }

            int destination = StackWriteOperand(instruction);
            if (destination == int.MinValue || opcode == Swap
                || opcode == GetList && instruction.Operands[2].Kind == BeamOperandParameters.Y)
            {
                return -1;
            }

            if (destination < 0 || instruction.Operands[destination].Kind != BeamOperandParameters.Y)
            {
                continue;
            }

            if (projection >= 0 || opcode != GetTupleElement || instruction.Operands[0].Kind != BeamOperandParameters.X
                || !SameProjectionRegister(instruction.Operands[destination], pending))
            {
                return -1;
            }

            projection = index;
        }

        return projection;
    }
}
