using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Allocation, int Width, int Slot, int Register)? PlanTerminalTupleFrameSlot(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        ref BeamProjectionFlow? flow,
        out HashSet<int>? heaps,
        bool recheckAfterCopyRemoval
    )
    {
        heaps = null;
        int allocation = -1;
        int call = -1;
        for (int index = entry; index < end; index++)
        {
            var instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case TrimFrame or CatchEnter or CatchEnd or TryEnter or TryEnd or TryCase or TryCaseEnd
                    or DebugLine or ExecutableLine:
                    return null;
                case Allocate or AllocateHeap:
                    if (allocation >= 0 || instruction.Operands[0].Value < 2)
                    {
                        return null;
                    }

                    allocation = index;
                    break;
                case LocalCall or ExternalCall:
                    if (call >= 0)
                    {
                        return null;
                    }

                    call = index;
                    break;
            }
        }

        if (allocation < 0 || recheckAfterCopyRemoval && body[allocation].Operands[0].Value != 2
            || call <= allocation || call + 2 >= end
            || body[call + 1].Definition.Opcode != IsTuple
            || body[call + 2] is not { Definition.Opcode: SelectTupleArity, Operands: [var selector, ..] }
            || selector is not { Kind: BeamOperandParameters.X, Value.IsZero: true })
        {
            return null;
        }

        var uses = new List<(int Index, bool Definition)>();
        const int slot = 0;
        int scratch = recheckAfterCopyRemoval ? 2 : 1;
        for (int index = entry; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            // Initializing a frame slot and defining a projection are distinct.
            // An argument save in the same slot belongs to an earlier lifetime.
            int destination = StackAllocationDestination(instruction);
            for (int operand = 0; operand < instruction.Operands.Count; operand++)
            {
                if (instruction.Operands[operand] is not { Kind: BeamOperandParameters.Y, Value.IsZero: true })
                {
                    continue;
                }

                bool writes = operand == destination || instruction.Definition.Opcode == GetList && operand == 2;
                if (index <= call + 2 || instruction.Operands[operand].TypeIndex.HasValue
                    || writes && instruction is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true }, _, _]
                    })
                {
                    return null;
                }

                uses.Add((index, writes));
            }
        }

        if (uses.Count < 2 || !uses[0].Definition
            || ReadDisjointFrameOwnership(body, entry, end) is not { } frames || frames.Uses.Count != 1
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

        flow ??= new BeamProjectionFlow(body);
        var liveHeaps = new HashSet<int>();
        int definition = 0;
        while (definition < uses.Count)
        {
            int lastRead = definition + 1;
            if (lastRead == uses.Count || uses[lastRead].Definition)
            {
                return null;
            }

            while (lastRead + 1 < uses.Count && !uses[lastRead + 1].Definition)
            {
                lastRead++;
            }

            int first = uses[definition].Index;
            int last = uses[lastRead].Index;
            if (!flow.CheckTerminalTupleFrameInterval(
                first,
                last,
                new BeamOperand(BeamOperandParameters.X, scratch),
                new BeamOperand(BeamOperandParameters.Y, slot),
                liveHeaps
            ) || recheckAfterCopyRemoval && !flow.HasInitializedTerminalTupleRoots(
                first,
                last,
                scratch,
                liveHeaps
            ))
            {
                return null;
            }

            definition = lastRead + 1;
        }

        if (liveHeaps.Count == 0)
        {
            return null;
        }

        heaps = liveHeaps;

        return (allocation, (int)body[allocation].Operands[0].Value, slot, scratch);
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool CheckTerminalTupleFrameInterval(
            int first,
            int last,
            BeamOperand scratch,
            BeamOperand slot,
            HashSet<int> heaps
        )
        {
            var live = new bool[last - first + 1];
            for (int index = last; index >= first; index--)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (index > first && predecessors[index].Any(from => from < first || from >= index)
                    || instruction.Operands.Any(operand => SameProjectionRegister(operand, scratch))
                    || opcode is not (Label or Line or GetTupleElement or TestHeap or UpdateRecord or PutTuple or PutList
                        or Move or Deallocate or Return or TailLocalCallWithFrame or TailExternalCallWithFrame)
                        && !IsEntryFramePredicate(opcode)
                    || opcode is (TailLocalCallWithFrame or TailExternalCallWithFrame)
                        && instruction.Operands[0].Value > scratch.Value)
                {
                    return false;
                }

                bool liveAfter = false;
                foreach (int target in successors[index])
                {
                    if (target < first || target > last)
                    {
                        if (!IsEntryTemporaryDead(target, scratch))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // The interval only supports forward edges, so this fact is final.
                        if (target <= index)
                        {
                            return false;
                        }

                        liveAfter |= live[target - first];
                    }
                }

                // The defining projection reads X0. All admitted operations apart from
                // test_heap keep X0 valid or replace it with a freshly constructed term.
                if (opcode == TestHeap)
                {
                    if (instruction.Operands[^1].Value > scratch.Value)
                    {
                        return false;
                    }

                    if (liveAfter)
                    {
                        heaps.Add(index);
                    }
                }

                if (opcode == Move && instruction.Operands is not [_, { Kind: BeamOperandParameters.X or BeamOperandParameters.Y }])
                {
                    return false;
                }

                if (opcode == Deallocate && liveAfter)
                {
                    return false;
                }

                live[index - first] = liveAfter || instruction.Operands.Any(operand => SameProjectionRegister(operand, slot));
            }

            return true;
        }

        internal bool HasInitializedTerminalTupleRoots(
            int first,
            int last,
            int scratch,
            IReadOnlySet<int> adjustedHeaps
        )
        {
            int prefix = (1 << scratch) - 1;
            var outgoing = new int[last - first + 1];
            for (int index = first; index <= last; index++)
            {
                // The defining projection reads the call result in X0. Do
                // not assume any additional X roots from the entry state.
                int known = 1;
                if (index != first)
                {
                    if (predecessors[index].Count == 0)
                    {
                        return false;
                    }

                    known = prefix;
                    foreach (int edge in predecessors[index])
                    {
                        known &= outgoing[edge - first];
                    }
                }

                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == TestHeap)
                {
                    int originalPrefix = (1 << (int)instruction.Operands[^1].Value) - 1;
                    if (adjustedHeaps.Contains(index))
                    {
                        // Existing heap roots were already valid. Prove the
                        // additional lower roots before retaining the scratch.
                        if ((known | originalPrefix) != prefix)
                        {
                            return false;
                        }

                        known = prefix;
                    }
                    else
                    {
                        known = originalPrefix;
                    }
                }
                else
                {
                    int destination = StackWriteOperand(instruction);
                    if (destination >= 0 && instruction.Operands[destination] is
                        { Kind: BeamOperandParameters.X } written && written.Value < scratch)
                    {
                        known |= 1 << (int)written.Value;
                    }
                }

                outgoing[index - first] = known;
            }

            return true;
        }
    }
}
