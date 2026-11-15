using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class BeamProjectionFlow
    {
        internal (bool RemoveSpill, Dictionary<int, BeamOperand> Uses)? PlanEntryRegisterUses(
            int entry,
            int save,
            int end,
            BeamOperand source,
            BeamOperand slot
        )
        {
            var uses = new List<int>();
            bool needsLifetimeAnalysis = false;
            bool hasOverwrite = false;
            for (int index = entry; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is TrimFrame or TryEnter or CatchEnter || instruction.Definition.Name == "debug_line")
                {
                    // These instructions change how later stack reads refer to the saved value.
                    return null;
                }

                if (index == save || index < save && opcode == InitYRegisters
                    || !instruction.Operands.Any(operand => SameProjectionRegister(operand, slot)))
                {
                    continue;
                }

                if (index < save || !ReadsEntryCopy(instruction, slot))
                {
                    needsLifetimeAnalysis = true;
                    hasOverwrite |= index > save && OverwritesEntryCopy(instruction, slot);
                    continue;
                }

                uses.Add(index);
            }

            if (needsLifetimeAnalysis)
            {
                if (!hasOverwrite || FindEntryCopyLifetimeUses(entry, save, end, slot) is not { } lifetimeUses)
                {
                    return null;
                }

                uses = lifetimeUses;
            }

            if (uses.Count == 0)
            {
                return null;
            }

            BigInteger original = EntryRegisterBit(source);
            var incoming = new BigInteger?[end - entry];
            var pending = new Queue<int>();
            incoming[0] = BigInteger.Zero;
            pending.Enqueue(entry);
            while (pending.TryDequeue(out int index))
            {
                BigInteger available = index == save
                    ? original
                    : TransferEntryRegisters(body[index], incoming[index - entry]!.Value, slot);
                foreach (int target in successors[index])
                {
                    if (target >= entry && target < end)
                    {
                        int next = target - entry;
                        BigInteger merged = incoming[next] is { } seen ? seen & available : available;
                        if (incoming[next] != merged)
                        {
                            incoming[next] = merged;
                            pending.Enqueue(target);
                        }
                    }
                }
            }

            var replacements = new Dictionary<int, BeamOperand>();
            var redundantLoads = new Dictionary<int, BeamOperand>();
            foreach (int use in uses)
            {
                BigInteger available = incoming[use - entry].GetValueOrDefault();
                BeamInstruction instruction = body[use];
                if (instruction.Definition.Opcode is GcBifOne or GcBifTwo or GcBifThree)
                {
                    available &= EntryRegisterPrefix(instruction.Operands[1]);
                }

                if (available.IsZero)
                {
                    continue;
                }

                replacements.Add(use, (available & original).IsZero
                    ? new BeamOperand(BeamOperandParameters.X, BigInteger.TrailingZeroCount(available))
                    : source);
                if (instruction is { Definition.Opcode: Move, Operands: [_, { TypeIndex: null } destination] }
                    && !(available & EntryRegisterBit(destination)).IsZero)
                {
                    redundantLoads.Add(use, destination);
                }
            }

            if (uses.Count == replacements.Count)
            {
                return (true, replacements);
            }

            return redundantLoads.Count == 0 ? null : (false, redundantLoads);
        }

        private List<int>? FindEntryCopyLifetimeUses(
            int entry,
            int save,
            int end,
            BeamOperand slot
        )
        {
            const int BeforeOrAfterLifetime = 1;
            const int DuringLifetime = 2;
            var seen = new int[end - entry];
            var pending = new Stack<(int Index, bool HoldsCopy)>();
            pending.Push((entry, false));
            while (pending.TryPop(out var state))
            {
                var (index, holdsCopy) = state;
                int mark = holdsCopy ? DuringLifetime : BeforeOrAfterLifetime;
                if ((seen[index - entry] & mark) != 0)
                {
                    continue;
                }

                seen[index - entry] |= mark;
                BeamInstruction instruction = body[index];
                if (index == save)
                {
                    holdsCopy = true;
                }
                else if (holdsCopy && OverwritesEntryCopy(instruction, slot))
                {
                    holdsCopy = false;
                }

                foreach (int target in successors[index])
                {
                    if (target >= entry && target < end)
                    {
                        pending.Push((target, holdsCopy));
                    }
                }
            }

            var uses = new List<int>();
            for (int index = entry; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                int mark = seen[index - entry];
                if (index == save || (mark & DuringLifetime) == 0
                    || !instruction.Operands.Any(operand => SameProjectionRegister(operand, slot))
                    || OverwritesEntryCopy(instruction, slot))
                {
                    continue;
                }

                // An instruction shared with a path that replaced the slot must keep reading Y.
                if (mark != DuringLifetime || !ReadsEntryCopy(instruction, slot))
                {
                    return null;
                }

                uses.Add(index);
            }

            return uses;
        }

        private static bool OverwritesEntryCopy(BeamInstruction instruction, BeamOperand slot)
        {
            if (instruction.Definition.Opcode == InitYRegisters)
            {
                return instruction.Operands.Any(operand => SameProjectionRegister(operand, slot));
            }

            if (instruction.Definition.Opcode is not (Move or GetTupleElement or GetHead or GetTail
                or GetList or PutList or PutTuple))
            {
                return false;
            }

            int destination = StackWriteOperand(instruction);
            bool writes = false;
            for (int index = 0; index < instruction.Operands.Count; index++)
            {
                if (!SameProjectionRegister(instruction.Operands[index], slot))
                {
                    continue;
                }

                if (index != destination && !(instruction.Definition.Opcode == GetList && index == 2))
                {
                    return false;
                }

                writes = true;
            }

            return writes;
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

        private static BigInteger TransferEntryRegisters(
            BeamInstruction instruction,
            BigInteger available,
            BeamOperand slot
        )
        {
            byte opcode = instruction.Definition.Opcode;
            var operands = instruction.Operands;
            if (opcode == TestHeap)
            {
                return available & EntryRegisterPrefix(operands[^1]);
            }

            if (opcode == Move)
            {
                BigInteger destination = EntryRegisterBit(operands[1]);
                return SameProjectionRegister(operands[0], slot) || !(available & EntryRegisterBit(operands[0])).IsZero
                    ? available | destination
                    : available & ~destination;
            }

            if (opcode == Swap)
            {
                BigInteger left = EntryRegisterBit(operands[0]);
                BigInteger right = EntryRegisterBit(operands[1]);
                if (left.IsZero || right.IsZero)
                {
                    return available & ~(left | right);
                }

                BigInteger movedLeft = (available & right).IsZero ? BigInteger.Zero : left;
                BigInteger movedRight = (available & left).IsZero ? BigInteger.Zero : right;
                return (available & ~(left | right)) | movedLeft | movedRight;
            }

            if (opcode is GcBifOne or GcBifTwo or GcBifThree)
            {
                available &= EntryRegisterPrefix(operands[1]);
            }

            if (opcode is GetTupleElement or GetHead or GetTail or GetList or PutList or PutTuple
                or BifZero or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree)
            {
                available &= ~EntryRegisterBit(operands[StackWriteOperand(instruction)]);
                return opcode == GetList ? available & ~EntryRegisterBit(operands[2]) : available;
            }

            if (opcode == InitYRegisters && operands.Any(operand => SameProjectionRegister(operand, slot)))
            {
                return BigInteger.Zero;
            }

            return opcode is Label or Line or Jump or SelectValue or SelectTupleArity or InitYRegisters
                || IsEntryFramePredicate(opcode) ? available : BigInteger.Zero;
        }

        private static BigInteger EntryRegisterBit(BeamOperand operand)
            => operand.Kind == BeamOperandParameters.X ? BigInteger.One << (int)operand.Value : BigInteger.Zero;

        private static BigInteger EntryRegisterPrefix(BeamOperand live)
            => (BigInteger.One << (int)live.Value) - BigInteger.One;
    }
}
