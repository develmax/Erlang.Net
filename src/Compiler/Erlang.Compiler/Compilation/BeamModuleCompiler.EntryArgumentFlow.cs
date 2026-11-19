using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class BeamProjectionFlow
    {
        internal (bool RemoveSpill, Dictionary<int, BeamOperand> Uses, Dictionary<int, (int Before, BeamOperand Source, bool Extended)> EarlyLoads)? PlanEntryRegisterUses(
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
                if (!hasOverwrite || FindEntryCopyLifetimeUses(
                    entry,
                    save,
                    end,
                    slot
                ) is not { } lifetimeUses)
                {
                    return null;
                }

                uses = lifetimeUses;
            }

            if (uses.Count == 0)
            {
                // The slot still needs initialization for GC until it is overwritten.
                return (true, [], []);
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
            var retainedUses = new Dictionary<int, BeamOperand>();
            var earlyLoads = new Dictionary<int, (int Before, BeamOperand Source, bool Extended)>();
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
                    if (use > save + 1 && predecessors[use] is [int previous] && previous == use - 1
                        && instruction is { Definition.Opcode: Move, Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } target] }
                        && !SameProjectionRegister(target, source)
                        && body[previous] is
                        {
                            Definition.Opcode: Move, Operands: [
                            {
                                Kind: BeamOperandParameters.LiteralValue or BeamOperandParameters.Integer
                                or BeamOperandParameters.Atom or BeamOperandParameters.Nil
                            }, var overwritten]
                        }
                        && SameProjectionRegister(overwritten, source)
                        && !(incoming[previous - entry].GetValueOrDefault() & original).IsZero)
                    {
                        earlyLoads.Add(use, (previous, source, Extended: false));
                    }
                    else if (!needsLifetimeAnalysis && uses.Count == 1
                        && FindEarlyEntryLoad(
                            entry,
                            save,
                            use,
                            source,
                            incoming
                        ) is int before)
                    {
                        earlyLoads.Add(use, (before, source, Extended: true));
                    }

                    continue;
                }

                BeamOperand replacement = (available & original).IsZero
                    ? new BeamOperand(BeamOperandParameters.X, BigInteger.TrailingZeroCount(available))
                    : source;
                replacements.Add(use, replacement);
                if (instruction is { Definition.Opcode: Move, Operands: [_, { TypeIndex: null } destination] }
                    && !(available & EntryRegisterBit(destination)).IsZero)
                {
                    retainedUses.Add(use, destination);
                }
                else if (instruction.Definition.Opcode is PutTuple or PutList
                    && !SameProjectionRegister(instruction.Operands[StackWriteOperand(instruction)], replacement)
                    && !instruction.Operands.Any(operand => operand.TypeIndex.HasValue && SameProjectionRegister(operand, slot)))
                {
                    // Keep the stack copy for later reads after a call clobbers X.
                    retainedUses.Add(use, replacement);
                }
            }

            if (uses.Count == replacements.Count + earlyLoads.Count)
            {
                return (true, replacements, earlyLoads);
            }

            return retainedUses.Count == 0 ? null : (false, retainedUses, []);
        }

        internal bool HasSingleStackCopyUse(
            int entry,
            int save,
            int end,
            BeamOperand slot,
            int use
        ) => FindEntryCopyLifetimeUses(
            entry,
            save,
            end,
            slot,
            knownRead: use
        ) is [var index] && index == use;

        private int? FindEarlyEntryLoad(
            int entry,
            int save,
            int use,
            BeamOperand source,
            BigInteger?[] incoming
        )
        {
            if (body[use] is not
                {
                    Definition.Opcode: Move,
                    Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } target]
                }
                || SameProjectionRegister(target, source))
            {
                return null;
            }

            BigInteger sourceBit = EntryRegisterBit(source);
            bool crossesConstruction = false;
            for (int before = use - 1; before > save; before--)
            {
                BeamInstruction instruction = body[before];
                if (predecessors[before + 1] is not [var predecessor] || predecessor != before
                    || instruction.Definition.Opcode is not (Line or Move or PutList or PutTuple)
                    || instruction.Operands.Any(operand => SameProjectionRegister(operand, target)))
                {
                    return null;
                }

                crossesConstruction |= instruction.Definition.Opcode is PutList or PutTuple;
                // Keep move-only tail schedules on the existing parallel-copy path.
                if (!(incoming[before - entry].GetValueOrDefault() & sourceBit).IsZero)
                {
                    return crossesConstruction || FeedsCallAfterClosure(before) || FeedsStraightCall(before) ? before : null;
                }
            }

            return null;

            bool FeedsStraightCall(int before)
            {
                if (body[before] is not { Definition.Opcode: Move, Operands: [_, var overwritten] }
                    || !SameProjectionRegister(overwritten, source))
                {
                    return false;
                }

                // This new route covers only an entry copy followed by straight
                // argument moves. A closure or collecting operation remains a barrier.
                for (int index = save + 1; index <= before; index++)
                {
                    if (body[index].Definition.Opcode is not (Move or Line)
                        || predecessors[index] is not [var predecessor] || predecessor != index - 1)
                    {
                        return false;
                    }
                }

                return FeedsOrdinaryCall(straightArguments: true);
            }

            bool FeedsCallAfterClosure(int before)
            {
                int producer = before - 1;
                while (producer > save && body[producer].Definition.Opcode == Line)
                {
                    producer--;
                }

                if (producer <= save || body[producer] is not { Definition.Opcode: MakeFun } closure)
                {
                    return false;
                }

                BeamOperand result = closure.Operands[StackWriteOperand(closure)];
                if (result.Kind != BeamOperandParameters.Y || SameProjectionRegister(result, body[save].Operands[1]))
                {
                    return false;
                }

                return FeedsOrdinaryCall();
            }

            bool FeedsOrdinaryCall(bool straightArguments = false)
            {
                int call = use + 1;
                while (call < entry + incoming.Length)
                {
                    BeamInstruction following = body[call];
                    if (following.Definition.Opcode == Line)
                    {
                        call++;

                        continue;
                    }

                    if (!straightArguments || following is not
                        {
                            Definition.Opcode: Move,
                            Operands:
                            [
                            {
                                Kind: BeamOperandParameters.X or BeamOperandParameters.LiteralValue
                                        or BeamOperandParameters.Integer or BeamOperandParameters.Atom
                                        or BeamOperandParameters.Nil,
                                TypeIndex: null
                            } read,
                            { Kind: BeamOperandParameters.X, TypeIndex: null } written
                            ]
                        } || predecessors[call] is not [var predecessor] || predecessor != call - 1
                        || SameProjectionRegister(read, target) || SameProjectionRegister(written, target))
                    {
                        break;
                    }

                    call++;
                }

                if (call >= entry + incoming.Length)
                {
                    return false;
                }

                const int SendArgumentCount = 2;
                BeamInstruction consumer = body[call];

                // Send is a body call with two implicit X arguments and requires an allocated frame.
                // CallFun's separate fun operand is excluded from this argument proof.
                return consumer.Definition.Opcode switch
                {
                    LocalCall or ExternalCall => target.Value < consumer.Operands[0].Value,
                    CallFun when straightArguments => target.Value < consumer.Operands[0].Value,
                    Send when straightArguments => target.Value < SendArgumentCount,
                    _ => false
                };
            }
        }

        private List<int>? FindEntryCopyLifetimeUses(
            int entry,
            int save,
            int end,
            BeamOperand slot,
            int knownRead = -1
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
                if (mark != DuringLifetime || index != knownRead && !ReadsEntryCopy(instruction, slot))
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
                or GetList or PutList or PutTuple or MakeFun))
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

            return opcode is Move or GetTupleElement or GetHead or GetTail or GetList or PutList or PutTuple or MakeFun
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

            if (opcode is GetTupleElement or GetHead or GetTail or GetList or PutList or PutTuple or MakeFun
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
