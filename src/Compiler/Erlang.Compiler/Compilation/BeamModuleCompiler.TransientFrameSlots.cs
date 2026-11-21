using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? PromoteTransientFrameSlots(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
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
            if (PlanTransientFrameSlot(body, entry, end) is not { } candidate)
            {
                continue;
            }

            var (slot, first, last, usedX, heaps) = candidate;
            int limit = Math.Min(BeamOperandParameters.MaximumRegister, usedX.Count + 2);
            IEnumerable<int> registers = heaps.Count == 0
                ? Enumerable.Range(0, limit)
                : [(int)body[heaps[0]].Operands[^1].Value];
            int register = registers.FirstOrDefault(
                x => x < BeamOperandParameters.MaximumRegister
                && !usedX.Contains(x)
                && (heaps.Count == 0 || x + 1 < BeamOperandParameters.MaximumRegister
                    && heaps.All(index => body[index].Operands[^1].Value == x))
                && (flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(
                    first,
                    new BeamOperand(BeamOperandParameters.X, x)
                ),
                -1
            );
            if (register < 0)
            {
                continue;
            }

            int from = body[first].Offset;
            int to = body[last + 1].Offset;
            while (operand < stackOperands.Count && stackOperands[operand].Start < from)
            {
                operand++;
            }

            while (operand < stackOperands.Count && stackOperands[operand].Start < to)
            {
                var (at, after, oldSlot) = stackOperands[operand++];
                if (oldSlot == slot)
                {
                    edits.Add(new(
                        at,
                        after,
                        XRegisterTag,
                        register
                    ));
                }
            }

            foreach (int heap in heaps)
            {
                int next = body[heap + 1].Offset;
                edits.Add(new(
                    next - CompactRegisterCountWidth(register),
                    next,
                    BeamWire.UnsignedTag,
                    register + 1
                ));
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        int copied = 0;
        using var output = new MemoryStream();
        foreach (var edit in edits.OrderBy(edit => edit.Start))
        {
            output.Write(code.Span[copied..edit.Start]);
            BeamCompactWriter.Write(output, edit.Tag, edit.Value);
            copied = edit.End;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Slot, int First, int Last, HashSet<int> UsedX, List<int> Heaps)? PlanTransientFrameSlot(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        int frame = -1;
        bool ordinaryAllocation = false;
        bool executableLine = false;
        bool dynamicBeforeFirstCall = false;
        int highestSlot = -1;
        int first = -1;
        int last = -1;
        int firstCall = end;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            executableLine |= instruction.Definition.Name == "executable_line";
            dynamicBeforeFirstCall |= firstCall == end
                && opcode is CallFun or CallFunTyped or DynamicApply or TailDynamicApply;
            if (opcode is TrimFrame or CatchEnter or TryEnter || instruction.Definition.Name == "debug_line")
            {
                return null;
            }

            if (opcode is Allocate or AllocateHeap)
            {
                if (frame >= 0)
                {
                    return null;
                }

                frame = (int)instruction.Operands[0].Value;
                ordinaryAllocation = opcode == Allocate;
            }
            else if (opcode is LocalCall or ExternalCall)
            {
                firstCall = Math.Min(firstCall, index);
            }

            if (opcode == InitYRegisters)
            {
                continue;
            }

            foreach (var operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y && operand.Value >= highestSlot)
                {
                    if (operand.Value > highestSlot)
                    {
                        highestSlot = (int)operand.Value;
                        first = index;
                    }

                    last = index;
                }
            }
        }

        if (highestSlot < 0 || highestSlot >= frame || firstCall == end)
        {
            return null;
        }

        bool beforeFirstCall = last < firstCall;
        if (first > firstCall || beforeFirstCall)
        {
            return PlanTransientSlotRange(
                body,
                highestSlot,
                first,
                last,
                beforeFirstCall
            );
        }

        // The highest slot crosses the first call. Consider lower projections
        // only in an ordinary frame, before any operation that can clobber them.
        if (!ordinaryAllocation || executableLine || dynamicBeforeFirstCall)
        {
            return null;
        }

        var lifetimes = new Dictionary<int, (int First, int Last)>();
        for (int index = start; index < end; index++)
        {
            if (body[index].Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            foreach (BeamOperand operand in body[index].Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y && operand.Value < highestSlot)
                {
                    int slot = (int)operand.Value;
                    int beginning = lifetimes.TryGetValue(slot, out var existing) ? existing.First : index;
                    lifetimes[slot] = (beginning, index);
                }
            }
        }

        foreach (var (slot, range) in lifetimes.OrderByDescending(entry => entry.Key))
        {
            if (range.Last < firstCall && body[range.First].Definition.Opcode == GetTupleElement
                && PlanTransientSlotRange(
                    body,
                    slot,
                    range.First,
                    range.Last,
                    beforeFirstCall: true
                ) is { } candidate)
            {
                return candidate;
            }
        }

        return null;
    }

    private static (int Slot, int First, int Last, HashSet<int> UsedX, List<int> Heaps)? PlanTransientSlotRange(
        IReadOnlyList<BeamInstruction> body,
        int slot,
        int first,
        int last,
        bool beforeFirstCall
    )
    {
        var usedX = new HashSet<int>();
        var heaps = new List<int>();
        for (int index = first; index <= last; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is not (Line or Move or GetTupleElement or GetHead or GetTail or GetList or PutList or PutTuple or TestHeap)
                && !IsEntryFramePredicate(opcode))
            {
                return null;
            }

            if (opcode == TestHeap)
            {
                if (beforeFirstCall)
                {
                    return null;
                }

                heaps.Add(index);
            }

            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (operand.Kind == BeamOperandParameters.X)
                {
                    usedX.Add((int)operand.Value);
                }
                else if (index == first && operand.Kind == BeamOperandParameters.Y && operand.Value == slot
                    && position != StackWriteOperand(instruction) && !(opcode == GetList && position == 2))
                {
                    // An earlier definition must not be required, including a read before a write.
                    return null;
                }
            }
        }

        return (slot, first, last, usedX, heaps);
    }
}
