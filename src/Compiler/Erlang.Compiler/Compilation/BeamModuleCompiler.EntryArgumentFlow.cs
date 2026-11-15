using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class BeamProjectionFlow
    {
        internal bool CanUseEntryRegister(
            int entry,
            int save,
            int end,
            BeamOperand source,
            BeamOperand slot
        )
        {
            int uses = 0;
            for (int index = entry; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is TrimFrame or TryEnter or CatchEnter || instruction.Definition.Name == "debug_line")
                {
                    // These instructions change how later stack reads refer to the saved value.
                    return false;
                }

                if (index == save || index < save && opcode == InitYRegisters
                    || !instruction.Operands.Any(operand => SameProjectionRegister(operand, slot)))
                {
                    continue;
                }

                if (index < save || !ReadsEntryCopy(instruction, slot))
                {
                    return false;
                }

                uses++;
            }

            if (uses == 0)
            {
                return false;
            }

            var visited = new HashSet<(int Index, bool Available)>();
            var pending = new Stack<(int Index, bool Available)>();
            pending.Push((entry, false));
            while (pending.TryPop(out var state))
            {
                if (!visited.Add(state))
                {
                    continue;
                }

                BeamInstruction instruction = body[state.Index];
                if (state.Index != save && state.Index > save
                    && instruction.Operands.Any(operand => SameProjectionRegister(operand, slot)))
                {
                    if (!state.Available || instruction.Definition.Opcode is (GcBifOne or GcBifTwo or GcBifThree)
                        && source.Value >= instruction.Operands[1].Value)
                    {
                        return false;
                    }

                    uses--;
                }

                bool available = state.Index == save
                    || state.Available && PreservesEntryRegister(instruction, source);
                foreach (int target in successors[state.Index])
                {
                    if (target >= entry && target < end)
                    {
                        pending.Push((target, available));
                    }
                }
            }

            // Every rewritten read must have been verified along at least one path.
            return uses == 0;
        }

        private static bool ReadsEntryCopy(BeamInstruction instruction, BeamOperand slot)
        {
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Swap or InitYRegisters)
            {
                return false;
            }

            int destination = StackWriteOperand(instruction);
            if (destination >= 0 && SameProjectionRegister(instruction.Operands[destination], slot)
                || opcode == GetList && SameProjectionRegister(instruction.Operands[2], slot))
            {
                return false;
            }

            return opcode is Move or GetTupleElement or GetHead or GetTail or GetList or PutList or PutTuple
                or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree
                or SelectValue or SelectTupleArity or BadMatch or CaseEnd or BadRecord
                || IsEntryFramePredicate(opcode);
        }

        private static bool PreservesEntryRegister(BeamInstruction instruction, BeamOperand source)
        {
            byte opcode = instruction.Definition.Opcode;
            var operands = instruction.Operands;
            if (opcode is Allocate or AllocateHeap or TestHeap)
            {
                return source.Value < operands[^1].Value;
            }

            if (opcode is Swap)
            {
                return !operands.Any(operand => SameProjectionRegister(operand, source));
            }

            if (opcode is GcBifOne or GcBifTwo or GcBifThree && source.Value >= operands[1].Value)
            {
                return false;
            }

            if (opcode is Move or GetTupleElement or GetHead or GetTail or GetList or PutList or PutTuple
                or BifZero or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree)
            {
                return !SameProjectionRegister(operands[StackWriteOperand(instruction)], source)
                    && (opcode != GetList || !SameProjectionRegister(operands[2], source));
            }

            return opcode is Label or Line or Jump or SelectValue or SelectTupleArity or InitYRegisters
                || IsEntryFramePredicate(opcode);
        }
    }
}
