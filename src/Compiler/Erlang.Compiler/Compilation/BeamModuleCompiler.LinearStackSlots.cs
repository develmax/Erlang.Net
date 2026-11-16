using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ReuseStackSlots(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands,
        bool useValueLiveness = false,
        IReadOnlySet<int>? closureEntries = null
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            int frame = useValueLiveness
                ? FindLinearFrameSuffix(body, entry, end)
                : FindEntryFrameAllocation(body, entry, end);
            var linear = PlanLinearStackSlots(
                body,
                frame,
                end,
                includeUnchanged: useValueLiveness
            );
            var allocation = linear is { } layout && !useValueLiveness
                ? (layout.Count, layout.Slots, Trim: ((int Index, int OriginalRemoved, int Kept)?)null)
                : !useValueLiveness || linear is not null
                    ? PlanBranchStackSlots(
                        body,
                        entry,
                        end,
                        linearAllocation: useValueLiveness ? frame : -1
                    )
                    : null;
            if (useValueLiveness && allocation is null)
            {
                int entryFrame = FindEntryFrameAllocation(body, entry, end);
                bool closure = entryFrame >= 0 && closureEntries?.Contains((int)body[entry].Operands[0].Value) == true;
                allocation = PlanSavedEntrySlotOrder(
                    body,
                    entryFrame,
                    end,
                    closure
                );
            }

            if (allocation is not { } plan)
            {
                continue;
            }

            for (int index = entry + 1; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                int next = body[index + 1].Offset;
                int originalShift = 0;
                int allocatedShift = 0;
                if (plan.Trim is { } trim && index >= trim.Index)
                {
                    originalShift = trim.OriginalRemoved;
                    allocatedShift = plan.Count - trim.Kept;
                    if (index == trim.Index)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        if (allocatedShift > 0)
                        {
                            output.WriteByte(TrimFrame);
                            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, allocatedShift);
                            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, trim.Kept);
                        }

                        copied = next;
                        continue;
                    }
                }

                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    // Clearing an old slot must leave its live alias intact.
                    int[] cleared = instruction.Operands.Skip(1)
                        .Select(slot => plan.Slots[(int)slot.Value + originalShift])
                        .Where(register => register >= 0 && (useValueLiveness || linear is null || !Enumerable.Range(0, plan.Slots.Length)
                            .Any(slot => plan.Slots[slot] == register
                                && linear.Value.Uses[slot].First < index && linear.Value.Uses[slot].Last > index)))
                        .Select(register => register - allocatedShift)
                        .Distinct().Order().ToArray();
                    output.Write(code.Span[copied..instruction.Offset]);
                    if (cleared.Length != 0)
                    {
                        output.WriteByte(InitYRegisters);
                        output.WriteByte(ExtendedListOperand);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, cleared.Length);
                        foreach (int register in cleared)
                        {
                            BeamCompactWriter.Write(output, YRegisterTag, register);
                        }
                    }

                    copied = next;
                    continue;
                }

                int size = LinearFrameSizeOperand(instruction.Definition.Opcode);
                if (size >= 0)
                {
                    int offset = instruction.Offset + 1;
                    var cursor = new BeamCursor(code[offset..]);
                    for (int position = 0; position < size; position++)
                    {
                        _ = BeamCompactReader.Read(cursor);
                    }

                    output.Write(code.Span[copied..(offset + cursor.Position)]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, plan.Count - allocatedShift);
                    _ = BeamCompactReader.Read(cursor);
                    copied = offset + cursor.Position;
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
                    BeamCompactWriter.Write(output, YRegisterTag, plan.Slots[slot + originalShift] - allocatedShift);
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

    private static (int Count, int[] Slots, (int Index, int OriginalRemoved, int Kept)? Trim)? PlanSavedEntrySlotOrder(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int end,
        bool closure
    )
    {
        if (allocation < 0)
        {
            return null;
        }

        int count = (int)body[allocation].Operands[0].Value;
        int live = (int)body[allocation].Operands[^1].Value;
        // A full closure frame can follow the evaluation order of lifted arguments.
        if (count < 2 || count > live || count >= end - allocation || (closure && count == live))
        {
            return null;
        }

        var entriesBySlot = new Dictionary<int, int>(count);
        var inputs = new HashSet<int>();
        for (int index = allocation + 1; index <= allocation + count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move, Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } input,
                    { Kind: BeamOperandParameters.Y } slot]
                }
                || input.Value >= live || slot.Value >= count
                || !inputs.Add((int)input.Value) || !entriesBySlot.TryAdd((int)slot.Value, (int)input.Value))
            {
                return null;
            }
        }

        for (int index = allocation + count + 1; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode is Allocate or AllocateHeap or TrimFrame or TryEnter or CatchEnter
                || instruction.Definition.Name is "debug_line" or "executable_line")
            {
                return null;
            }
        }

        // OTP reverses the reserved argument slots when numbering the frame.
        var slots = new int[count];
        int destination = 0;
        foreach (int slot in entriesBySlot.Keys.OrderByDescending(slot => entriesBySlot[slot]))
        {
            slots[slot] = destination++;
        }

        if (slots.Select((slot, index) => slot == index).All(same => same))
        {
            return null;
        }

        return (count, slots, null);
    }

    private static int FindLinearFrameSuffix(IReadOnlyList<BeamInstruction> body, int entry, int end)
    {
        if (entry >= end || body[entry].Definition.Opcode != Label)
        {
            return -1;
        }

        for (int index = entry + 1; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode is Allocate or AllocateHeap)
            {
                return index;
            }

            if (LinearFrameSizeOperand(instruction.Definition.Opcode) >= 0
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
            {
                return -1;
            }
        }

        return -1;
    }

    private static (int Count, int[] Slots, (int First, int Last)[] Uses)? PlanLinearStackSlots(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int end,
        bool includeUnchanged = false
    )
    {
        if (allocation < 0)
        {
            return null;
        }

        int frame = (int)body[allocation].Operands[0].Value;
        if (frame < 2 || frame > BeamOperandParameters.MaximumRegister)
        {
            return null;
        }

        var used = new (int First, int Last)[frame];
        Array.Fill(used, (-1, -1));
        var defined = new bool[frame];
        bool released = false;
        bool terminal = false;
        for (int index = allocation + 1; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Label or Line)
            {
                // The next function's info label follows the terminal instruction.
                // Earlier labels could allow a branch to bypass the allocation.
                if (includeUnchanged && opcode == Label && !terminal)
                {
                    return null;
                }

                continue;
            }

            if (terminal || opcode is Allocate or AllocateHeap or TrimFrame or CatchEnter or TryEnter
                || opcode is RemoveMessage or ReceiveTimeout or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout
                || instruction.Definition.Name == "debug_line"
                || opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame)
                    && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label))
            {
                return null;
            }

            int frameOperand = LinearFrameSizeOperand(opcode);
            if (frameOperand >= 0)
            {
                if (released || instruction.Operands[frameOperand].Value != frame)
                {
                    return null;
                }

                released = true;
            }

            terminal = EndsBeamBlock(opcode) || opcode == IfEnd;
            if (instruction.Operands.All(operand => operand.Kind != BeamOperandParameters.Y))
            {
                continue;
            }

            if (released)
            {
                return null;
            }

            if (opcode == InitYRegisters)
            {
                foreach (var slot in instruction.Operands.Skip(1))
                {
                    if (slot.Value >= frame)
                    {
                        return null;
                    }

                    defined[(int)slot.Value] = false;
                }

                continue;
            }

            int destination = StackAllocationDestination(instruction);
            if (destination == int.MinValue && !IsEntryFramePredicate(opcode))
            {
                return null;
            }

            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                var register = instruction.Operands[position];
                if (register.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                int slot = (int)register.Value;
                bool writes = position == destination || opcode == GetList && position == 2;
                // Sources read the old values, including a source that also appears as a destination.
                if (slot >= frame || !writes && !defined[slot])
                {
                    return null;
                }

                used[slot] = (used[slot].First < 0 ? index : used[slot].First, index);
            }

            if (destination >= 0 && instruction.Operands[destination] is { Kind: BeamOperandParameters.Y } written)
            {
                defined[(int)written.Value] = true;
            }

            if (opcode == GetList && instruction.Operands[2] is { Kind: BeamOperandParameters.Y } tail)
            {
                defined[(int)tail.Value] = true;
            }
        }

        if (!released || !terminal)
        {
            return null;
        }

        var slots = new int[frame];
        Array.Fill(slots, -1);
        var last = new List<int>();
        foreach (int slot in Enumerable.Range(0, frame).Where(slot => used[slot].First >= 0).OrderBy(slot => used[slot].First))
        {
            int register = last.FindIndex(endOfUse => endOfUse < used[slot].First);
            if (register < 0)
            {
                register = last.Count;
                last.Add(used[slot].Last);
            }
            else
            {
                last[register] = used[slot].Last;
            }

            slots[slot] = register;
        }

        if (last.Count == 0 || !includeUnchanged && last.Count >= frame)
        {
            return null;
        }

        // OTP reverses the linear-scan stack register numbering when laying out the frame.
        for (int slot = 0; slot < slots.Length; slot++)
        {
            if (slots[slot] >= 0)
            {
                slots[slot] = last.Count - slots[slot] - 1;
            }
        }

        return (last.Count, slots, used);
    }

    private static int StackAllocationDestination(BeamInstruction instruction) => instruction.Definition.Opcode switch
    {
        FloatConvert or GetBinaryTail or GetBinaryPosition => 1,
        PutMapAssoc or PutMapExact => 2,
        StartBinaryMatch or StartBinaryMatchKnown => 3,
        CreateBinary or GetBinaryUtf8 or GetBinaryUtf16 or GetBinaryUtf32 => 4,
        GetBinaryInteger or GetBinaryFloat or GetBinarySegment => 6,
        MatchBinaryString or SkipBinaryBits or SetBinaryPosition => -1,
        _ => StackWriteOperand(instruction)
    };

    private static int StackWriteOperand(BeamInstruction instruction) => instruction.Definition.Opcode switch
    {
        Move or GetHead or GetTail or MakeFun => 1,
        PutList or GetTupleElement => 2,
        PutTuple => 0,
        UpdateRecord => 3,
        BifZero or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree => instruction.Operands.Count - 1,
        GetList => 1,
        BadMatch or BadRecord or CaseEnd or Swap or CallFunTyped => -1,
        _ => int.MinValue
    };

    private static int LinearFrameSizeOperand(byte opcode) => opcode switch
    {
        Allocate or AllocateHeap or Deallocate => 0,
        TailDynamicApply => 1,
        TailLocalCallWithFrame or TailExternalCallWithFrame => 2,
        _ => -1
    };
}
