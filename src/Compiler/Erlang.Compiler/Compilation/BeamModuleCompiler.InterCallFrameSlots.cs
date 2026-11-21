using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ReadOnlyMemory<byte> PromoteLateInterCallFrameSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return code;
        }

        byte[]? promoted = ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            ? PromoteInterCallFrameSlots(code, decoded.Body, decoded.StackOperands)
            : PromoteFrameSlots(
                code,
                decoded.Body,
                decoded.StackOperands,
                allowTerminalTuple: true
            );

        return promoted is not null ? promoted : code;
    }

    private static byte[]? PromoteInterCallFrameSlots(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    ) => PromoteFrameSlots(
        code,
        body,
        stackOperands,
        allowTerminalTuple: false
    );

    private static byte[]? PromoteFrameSlots(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        bool allowTerminalTuple,
        IReadOnlySet<(int Atom, int Arity)>? recheckAfterCopyRemoval = null
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
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
            if (recheckAfterCopyRemoval is not null && (entry == 0
                || !recheckAfterCopyRemoval.Contains(ReadCallCopyFunction(body[entry - 1]))))
            {
                continue;
            }

            HashSet<int>? heaps = null;
            var frame = recheckAfterCopyRemoval is null
                ? PlanInterCallFrameSlot(
                    body,
                    entry,
                    end,
                    ref flow
                )
                : null;
            if (frame is null && allowTerminalTuple)
            {
                frame = PlanTerminalTupleFrameSlot(
                    body,
                    entry,
                    end,
                    ref flow,
                    out heaps,
                    recheckAfterCopyRemoval is not null
                );
            }

            if (frame is not { } plan)
            {
                continue;
            }

            var (allocation, width, slot, register) = plan;
            for (int index = allocation; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                int offset = instruction.Offset;
                int next = body[index + 1].Offset;
                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    if (instruction.Operands.Skip(1).Any(operand => operand.Value >= slot))
                    {
                        output.Write(code.Span[copied..offset]);
                        WriteFrameInitialization(
                            output,
                            instruction.Operands.Skip(1)
                            .Select(operand => (int)operand.Value)
                            .Where(value => value != slot)
                            .Select(value => value > slot ? value - 1 : value)
                            .ToArray()
                        );
                        copied = next;
                    }

                    continue;
                }

                if (heaps?.Contains(index) == true)
                {
                    copied = WritePostCallProjectionOperand(
                        output,
                        code,
                        offset,
                        copied,
                        1,
                        register + 1
                    );
                }

                int widthOperand = LinearFrameSizeOperand(instruction.Definition.Opcode);
                if (widthOperand >= 0)
                {
                    copied = WritePostCallProjectionOperand(
                        output,
                        code,
                        offset,
                        copied,
                        widthOperand,
                        width - 1
                    );
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < offset)
                {
                    operand++;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                {
                    var (from, to, old) = stackOperands[operand++];
                    if (old < slot)
                    {
                        continue;
                    }

                    // The decoder records the compact register, leaving any type envelope intact.
                    output.Write(code.Span[copied..from]);
                    BeamCompactWriter.Write(output, old == slot ? XRegisterTag : YRegisterTag, old == slot ? register : old - 1);
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

    private static (int Allocation, int Width, int Slot, int Register)? PlanInterCallFrameSlot(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        int allocation = -1;
        var calls = new List<int>();
        var accesses = new Dictionary<int, (int First, int Last, int Writes, bool Read)>();
        for (int index = entry; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is TrimFrame or CatchEnter or TryEnter or CatchEnd or TryEnd or TryCase or TryCaseEnd
                || instruction.Definition.Name is "debug_line" or "executable_line")
            {
                return null;
            }

            if (opcode is Allocate or AllocateHeap)
            {
                if (allocation >= 0 || instruction.Operands[0].Value < 2)
                {
                    return null;
                }

                allocation = index;
            }
            else if (opcode is LocalCall or ExternalCall)
            {
                calls.Add(index);
            }

            if (opcode == InitYRegisters)
            {
                continue;
            }

            int destination = StackWriteOperand(instruction);
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                if (instruction.Operands[position] is not { Kind: BeamOperandParameters.Y } saved)
                {
                    continue;
                }

                int slot = (int)saved.Value;
                bool writes = position == destination || opcode == GetList && position == 2;
                if (accesses.TryGetValue(slot, out var usage))
                {
                    accesses[slot] = (usage.First, index, usage.Writes + (writes ? 1 : 0), usage.Read || !writes);
                }
                else
                {
                    accesses.Add(slot, (index, index, writes ? 1 : 0, !writes));
                }
            }
        }

        if (allocation < 0 || calls.Count < 2 || accesses.Count < 2)
        {
            return null;
        }

        int highest = accesses.Keys.Max();
        int width = (int)body[allocation].Operands[0].Value;
        if (highest >= width)
        {
            return null;
        }

        (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
        foreach (var (slot, use) in accesses.OrderByDescending(entry => entry.Key))
        {
            if (slot == highest || use.Writes != 1 || !use.Read || use.First == use.Last
                || !IsInterCallValueProducer(body[use.First]))
            {
                continue;
            }

            int previous = calls.FindLastIndex(call => call < use.First);
            if (previous < 0 || previous + 1 == calls.Count || use.Last >= calls[previous + 1])
            {
                continue;
            }

            int firstCall = calls[previous];
            int secondCall = calls[previous + 1];
            if (firstCall <= allocation || Enumerable.Range(firstCall + 1, secondCall - firstCall - 1)
                    .Any(index => !IsInterCallSliceInstruction(body[index].Definition.Opcode))
                || PlanTransientSlotRange(
                    body,
                    slot,
                    use.First,
                    use.Last,
                    beforeFirstCall: false
                ) is not { } range
                || range.Heaps.Count != 0)
            {
                continue;
            }

            ownership ??= ReadDisjointFrameOwnership(body, entry, end);
            if (ownership is not { } frames || frames.Uses.Count != 1
                || !CanRewriteInterCallFrame(
                    body,
                    entry,
                    allocation,
                    end,
                    frames.Owners
                ))
            {
                return null;
            }

            if (frames.Owners[firstCall - entry] != allocation || frames.Owners[secondCall - entry] != allocation)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            int limit = Math.Min(BeamOperandParameters.MaximumRegister, range.UsedX.Count + 2);
            for (int register = 0; register < limit; register++)
            {
                if (!range.UsedX.Contains(register) && flow.IsBranchTemporaryDead(
                    use.First,
                    new BeamOperand(BeamOperandParameters.X, register)
                ))
                {
                    return (allocation, width, slot, register);
                }
            }
        }

        return null;
    }

    private static bool CanRewriteInterCallFrame(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int allocation,
        int end,
        int[] owners
    )
    {
        int ownFailure = EntryFrameFailureLabel(body, entry);
        for (int index = entry; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (index == allocation)
            {
                continue;
            }

            bool owned = owners[index - entry] == allocation;
            if ((!owned && (LinearFrameSizeOperand(opcode) >= 0
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y)))
                || owned && opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame)
                    && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                        && operand.Value == ownFailure))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsInterCallValueProducer(BeamInstruction instruction) =>
        instruction.Definition.Opcode is GetTupleElement or GetList or GetHead or GetTail
        || instruction is { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.X }, _] };

    private static bool IsInterCallSliceInstruction(byte opcode) =>
        opcode is Line or Move or GetTupleElement or GetList or GetHead or GetTail or PutTuple or PutList
            or TestHeap or InitYRegisters || IsEntryFramePredicate(opcode);
}
