using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CoalesceRetiredRecordTailSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(IsTaggedTuple) || !code.Span.Contains(TrimFrame)
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
            coalesceRetiredRecordTail: true
        );
    }

    private static (int Size, int[] Slots)?[]? PlanRetiredRecordTailSlots(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        if (start + 8 >= end || !HasUnprotectedCallKillSchedule(body, start)
            || ReadDisjointFrameOwnership(
                body,
                start,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } frame)
        {
            return null;
        }

        var low = new BeamOperand(BeamOperandParameters.Y, 0);
        for (int index = start + 3; index + 3 < end; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } carrier, var field, var destination]
                } || destination != low || carrier == low
                || body[index - 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var previousCarrier, var previousField, _]
                } || carrier != previousCarrier || field.Value != previousField.Value + 1)
            {
                continue;
            }

            int check = index - 1;
            while (check > start && body[check] is
                { Definition.Opcode: GetTupleElement, Operands: [var same, _, var written] }
                && same == carrier && written != carrier && written != low)
            {
                check--;
            }

            int allocation = frame.Owners[index - start];
            if (allocation < start || body[allocation].Definition.Opcode is not (Allocate or AllocateHeap)
                || body[check] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var tested, var arity, _]
                } || tested != carrier || arity.Value != field.Value + 1
                || frame.Owners[check - start] != allocation || frame.Uses[allocation].Length < 3)
            {
                continue;
            }

            int size = frame.Uses[allocation].Length - 1;
            int use = -1;
            bool hasTrim = false;
            bool simple = true;
            for (int step = start; step < end; step++)
            {
                if (frame.Owners[step - start] != allocation || step == index)
                {
                    continue;
                }

                BeamInstruction instruction = body[step];
                if (instruction.Definition.Opcode == TrimFrame)
                {
                    if (instruction.Operands[0].Value < 2)
                    {
                        simple = false;
                        break;
                    }

                    hasTrim = true;
                }

                if (!instruction.Operands.Any(operand => SameProjectionRegister(operand, low)))
                {
                    continue;
                }

                if (instruction.Definition.Opcode == InitYRegisters && step < index)
                {
                    continue;
                }

                if (use >= 0 || step <= index || instruction is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }]
                    } || source != low)
                {
                    simple = false;
                    break;
                }

                use = step;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!simple || !hasTrim || use < 0 || !flow.ProveRetiredRecordTailSlot(
                index,
                use,
                carrier,
                start,
                allocation,
                frame.Owners
            ))
            {
                continue;
            }

            int[] compact = Enumerable.Range(-1, size + 1).ToArray();
            int[] alias = (int[])compact.Clone();
            alias[0] = compact[(int)carrier.Value];
            var plans = new (int Size, int[] Slots)?[end - start];
            for (int step = allocation; step < end; step++)
            {
                if (step == allocation || frame.Owners[step - start] == allocation)
                {
                    plans[step - start] = (size, step == index || step == use ? alias : compact);
                }
            }

            return plans;
        }

        return null;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool ProveRetiredRecordTailSlot(
            int projection,
            int use,
            BeamOperand carrier,
            int start,
            int allocation,
            IReadOnlyList<int> owners
        )
        {
            // Every path to the last field use must include its winning
            // definition, with the carrier untouched between the two.
            var seen = new HashSet<int>();
            var work = new Stack<int>(predecessors[use]);
            if (work.Count == 0)
            {
                return false;
            }

            while (work.TryPop(out int index))
            {
                if (index == projection || !seen.Add(index))
                {
                    continue;
                }

                if (index <= projection || owners[index - start] != allocation || predecessors[index].Count == 0
                    || body[index].Operands.Any(operand => SameProjectionRegister(operand, carrier))
                    || predecessors[index].Any(previous => previous >= index))
                {
                    return false;
                }

                foreach (int previous in predecessors[index])
                {
                    work.Push(previous);
                }
            }

            // The earlier tuple can be discarded only if every outgoing path
            // releases its frame or kills the old carrier before it reads it.
            seen.Clear();
            if (successors[projection] is not [var first] || first != projection + 1)
            {
                return false;
            }

            work.Push(first);
            while (work.TryPop(out int index))
            {
                if (!seen.Add(index))
                {
                    continue;
                }

                if (index < start || index - start >= owners.Count || owners[index - start] != allocation)
                {
                    return false;
                }

                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode is Deallocate or TailLocalCallWithFrame or TailExternalCallWithFrame
                    || instruction.Definition.Opcode == TrimFrame && instruction.Operands[0].Value > carrier.Value)
                {
                    continue;
                }

                if (instruction.Definition.Opcode == InitYRegisters
                    && instruction.Operands.Any(operand => SameProjectionRegister(operand, carrier)))
                {
                    continue;
                }

                if (instruction is { Definition.Opcode: Move or GetTupleElement, Operands: [var source, .., var written] }
                    && SameProjectionRegister(written, carrier) && !SameProjectionRegister(source, carrier))
                {
                    continue;
                }

                if (instruction.Operands.Any(operand => SameProjectionRegister(operand, carrier))
                    || successors[index].Any(next => next <= index))
                {
                    return false;
                }

                foreach (int next in successors[index])
                {
                    work.Push(next);
                }
            }

            return true;
        }
    }
}
