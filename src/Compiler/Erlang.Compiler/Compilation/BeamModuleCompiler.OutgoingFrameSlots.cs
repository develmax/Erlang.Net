using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record OutgoingFrameSlots(
        (int Size, int[] Slots)?[] Layouts,
        IReadOnlyDictionary<int, BeamOperand> Definitions,
        IReadOnlySet<int> Reloads
    );

    private byte[]? CompactTransientOutgoingSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(Move)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteDisjointFrameLayouts(
            code,
            decoded.Body,
            decoded.StackOperands,
            atomicOnly: false,
            completeTrimsOnly: false,
            entriesToCompact: null,
            reuseOccupiedEntrySlots: false,
            onlyAllocation: null,
            transientOutgoingOnly: true
        );
    }

    private static OutgoingFrameSlots? PlanTransientOutgoingSlots(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        int allocation = -1;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (instruction.Definition.Name is "debug_line" or "executable_line" or "recv_mark" or "recv_set"
                || opcode is TrimFrame or CatchEnter or TryEnter or LoopReceive or LoopReceiveEnd
                    or Wait or WaitTimeout or RemoveMessage or ReceiveTimeout)
            {
                return null;
            }

            if (opcode is Allocate or AllocateHeap)
            {
                if (allocation >= 0)
                {
                    return null;
                }

                allocation = index;
            }
        }

        if (allocation < 0 || body[allocation].Operands[0].Value < 2
            || body[allocation].Operands[0].Value > BeamOperandParameters.MaximumRegister)
        {
            return null;
        }

        int width = (int)body[allocation].Operands[0].Value;
        int slot = width - 1;
        var definitions = new Dictionary<int, BeamOperand>();
        var reloads = new HashSet<int>();
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == InitYRegisters || !MentionsStackSlot(instruction, slot))
            {
                continue;
            }

            if (!reloads.Contains(index))
            {
                if (!IsOutgoingSlotDefinition(instruction, slot)
                    || FindOutgoingSlotReload(
                        body,
                        index,
                        end,
                        slot
                    ) is not { } reload)
                {
                    return null;
                }

                definitions.Add(index, reload.Destination);
                reloads.Add(reload.Index);
            }
        }

        if (definitions.Count == 0 || ReadDisjointFrameOwnership(body, start, end) is not { } ownership)
        {
            return null;
        }

        // Every use of the highest slot has been classified before shrinking the owner.
        int[] slots = Enumerable.Range(0, width).ToArray();
        slots[slot] = -1;
        var layouts = new (int Size, int[] Slots)?[end - start];
        for (int index = start; index < end; index++)
        {
            if (index == allocation || ownership.Owners[index - start] == allocation)
            {
                layouts[index - start] = (slot, slots);
            }
            else if (MentionsStackSlot(body[index], slot) || LinearFrameSizeOperand(body[index].Definition.Opcode) >= 0)
            {
                return null;
            }
        }

        return new(
            layouts,
            definitions,
            reloads
        );
    }

    private static (int Index, int Call, BeamOperand Destination)? FindOutgoingSlotReload(
        IReadOnlyList<BeamInstruction> body,
        int definition,
        int end,
        int slot
    )
    {
        for (int index = definition + 1; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                } && source.Value == slot)
            {
                for (int before = definition + 1; before < index; before++)
                {
                    if (MentionsOutgoingX(body[before], destination))
                    {
                        return null;
                    }
                }

                return FindOutgoingSlotCall(
                    body,
                    index + 1,
                    end,
                    destination
                ) is int call ? (index, call, destination) : null;
            }

            if (instruction.Definition.Opcode is not (Move or Line) || MentionsStackSlot(instruction, slot))
            {
                return null;
            }
        }

        return null;
    }

    private static int? FindOutgoingSlotCall(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        BeamOperand destination
    )
    {
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is LocalCall or ExternalCall or TailLocalCall or TailExternalCall
                or TailLocalCallWithFrame or TailExternalCallWithFrame or DynamicApply or TailDynamicApply)
            {
                int selectors = opcode is DynamicApply or TailDynamicApply ? 2 : 0;

                return destination.Value < instruction.Operands[0].Value + selectors ? index : null;
            }

            if (opcode is not (Move or Line) || MentionsOutgoingX(instruction, destination))
            {
                return null;
            }
        }

        return null;
    }

    private static bool IsOutgoingSlotDefinition(BeamInstruction instruction, int slot) =>
        instruction is
        {
            Definition.Opcode: Move,
            Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null }, { Kind: BeamOperandParameters.Y, TypeIndex: null } target]
        } && target.Value == slot
        || instruction is
        {
            Definition.Opcode: GetTupleElement,
            Operands:
            [
            { Kind: BeamOperandParameters.X, TypeIndex: null },
            { Kind: BeamOperandParameters.Unsigned },
            { Kind: BeamOperandParameters.Y, TypeIndex: null } projection
            ]
        } && projection.Value == slot;

    private static bool MentionsStackSlot(BeamInstruction instruction, int slot) =>
        instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y && operand.Value == slot);

    private static bool MentionsOutgoingX(BeamInstruction instruction, BeamOperand destination) =>
        instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.X && operand.Value == destination.Value);
}
