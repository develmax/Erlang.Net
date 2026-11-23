using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class BeamProjectionFlow
    {
        private readonly IReadOnlyList<BeamInstruction> body;
        private readonly List<int>[] predecessors;
        private readonly Dictionary<int, int> labels;
        private readonly int[][] successors;

        internal BeamProjectionFlow(IReadOnlyList<BeamInstruction> body)
        {
            this.body = body;
            labels = body.Select((instruction, index) => (instruction, index))
                .Where(entry => entry.instruction.Definition.Opcode == Label)
                .ToDictionary(entry => (int)entry.instruction.Operands[0].Value, entry => entry.index);
            predecessors = new List<int>[body.Count];
            successors = new int[body.Count][];
            for (int index = 0; index < body.Count; index++)
            {
                predecessors[index] = [];
            }

            for (int index = 0; index < body.Count; index++)
            {
                var instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                var targets = new List<int>();
                if (opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame))
                {
                    foreach (var operand in instruction.Operands)
                    {
                        if (operand.Kind == BeamOperandParameters.Label
                            && labels.TryGetValue((int)operand.Value, out int target))
                        {
                            targets.Add(target);
                        }
                    }
                }

                if (index + 1 < body.Count && FallsThrough(opcode))
                {
                    targets.Add(index + 1);
                }

                successors[index] = targets.ToArray();
                foreach (int target in targets)
                {
                    predecessors[target].Add(index);
                }
            }
        }

        internal bool TryGetSingleForwardTarget(int from, int label, out int target) =>
            labels.TryGetValue(label, out target) && target > from
            && predecessors[target].Count == 1 && predecessors[target][0] == from;

        internal int FindCheckedCarrierFallback(int target, BeamOperand carrier)
        {
            if (body[target] is not { Definition.Opcode: Label, Operands: [var label] }
                || predecessors[target] is not [var check, var select]
                || check + 1 != select || select >= target
                || body[check] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var rejected, var tested, _, _]
                }
                || rejected.Value != label.Value || tested != carrier
                || body[select] is not { Definition.Opcode: SelectValue, Operands: [_, var fallback, ..] }
                || fallback.Value != label.Value)
            {
                return -1;
            }

            return check;
        }

        internal bool TryGetExclusiveForwardTarget(
            int first,
            int second,
            int label,
            out int target
        ) =>
            TryGetForwardLabel(second, label, out target)
            && predecessors[target].Count == 2 && predecessors[target].Contains(first)
            && predecessors[target].Contains(second);

        internal bool TryGetExclusiveForwardTarget(
            int first,
            int second,
            int third,
            int label,
            out int target
        ) =>
            TryGetForwardLabel(third, label, out target)
            && predecessors[target].Count == 3
            && predecessors[target].Contains(first)
            && predecessors[target].Contains(second)
            && predecessors[target].Contains(third);

        internal bool TryGetForwardLabel(int from, int label, out int target) =>
            labels.TryGetValue(label, out target) && target > from;

        internal bool ReachesLinearFrameUnframed(int entry, int allocation)
        {
            if (entry >= allocation || body[entry].Definition.Opcode != Label)
            {
                return false;
            }

            // The frame body has no incoming labels. Check every predecessor
            // of its allocation, even if other arms share or trim a frame.
            var seen = new HashSet<int> { allocation };
            var work = new Stack<int>();
            work.Push(allocation);
            while (work.TryPop(out int index))
            {
                if (index == entry)
                {
                    if (predecessors[index].Any(predecessor => predecessor >= entry))
                    {
                        return false;
                    }

                    continue;
                }

                if (predecessors[index].Count == 0)
                {
                    return false;
                }

                foreach (int predecessor in predecessors[index])
                {
                    if (predecessor < entry || predecessor >= index)
                    {
                        return false;
                    }

                    if (!seen.Add(predecessor))
                    {
                        continue;
                    }

                    BeamInstruction instruction = body[predecessor];
                    byte opcode = instruction.Definition.Opcode;
                    if (opcode is not (Label or Line or Move or GetTupleElement or GetList or GetHead or GetTail
                            or Jump or SelectValue or SelectTupleArity) && !IsEntryFramePredicate(opcode)
                        || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
                    {
                        return false;
                    }

                    work.Push(predecessor);
                }
            }

            return seen.Contains(entry);
        }

        internal bool IsSingleForwardSelectionTarget(int target) =>
            predecessors[target] is [var source] && source < target
            && body[source].Definition.Opcode is (SelectValue or SelectTupleArity);

        internal bool CanEliminate(int projection, int continuation, BeamOperand slot) =>
            IsDead(continuation, slot) && IsInitialized(projection, slot);

        internal bool IsInitialized(
            int projection,
            BeamOperand slot,
            int closedCatchEnter = -1,
            int closedCatchEnd = -1,
            bool allowFramePredicates = false,
            bool requireClearedSlot = false,
            bool allowOwnedCollectingWrite = false,
            bool allowUnchangedTryEntry = false,
            bool allowRegisterRecordUpdates = false
        )
        {
            var work = new Stack<(int Index, bool Initialized)>();
            var seen = new HashSet<(int Index, bool Initialized)>();
            bool foundFrame = false;
            work.Push((projection, false));
            while (work.TryPop(out var state))
            {
                if (!seen.Add(state))
                {
                    continue;
                }

                if (predecessors[state.Index].Count == 0)
                {
                    return false;
                }

                foreach (int previous in predecessors[state.Index])
                {
                    var instruction = body[previous];
                    bool initialized = state.Initialized;
                    switch (instruction.Definition.Opcode)
                    {
                        case Allocate:
                        case AllocateHeap:
                            if (!initialized)
                            {
                                return false;
                            }

                            foundFrame = true;
                            continue;
                        case InitYRegisters:
                            initialized |= instruction.Operands.Any(operand => SameProjectionRegister(operand, slot));
                            break;
                        case Move:
                        case GetTupleElement:
                        case GetHead:
                        case GetTail:
                        case GetList:
                            bool writes = SameProjectionRegister(instruction.Operands[^1], slot)
                                || instruction.Definition.Opcode == GetList && SameProjectionRegister(instruction.Operands[1], slot);
                            if (writes && requireClearedSlot && !initialized)
                            {
                                return false;
                            }

                            initialized |= writes;
                            break;
                        case GcBifTwo when allowOwnedCollectingWrite && instruction.Operands is
                            [{ Value.IsZero: true }, _, _,
                            { Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.Integer },
                            { Kind: BeamOperandParameters.X }]:
                            // The unprotected collecting instruction does not define or clear a Y slot.
                            break;
                        case GcBifOne when allowOwnedCollectingWrite && instruction.Operands is
                            [{ Value.IsZero: true }, { Value.IsZero: true }, _,
                            { Kind: BeamOperandParameters.Y, TypeIndex: null } input, var result]
                            && input == result:
                            // The aliased operand must already be initialized. Reading
                            // backwards still needs an earlier definition of the slot.
                            break;
                        case TryEnter when allowUnchangedTryEntry
                            && !SameProjectionRegister(instruction.Operands[0], slot):
                        case CatchEnter when previous == closedCatchEnter
                            && !SameProjectionRegister(instruction.Operands[0], slot):
                        case CatchEnd when previous == closedCatchEnd
                            && !SameProjectionRegister(instruction.Operands[0], slot):
                        case LocalCall:
                        case ExternalCall:
                        case Label:
                        case Line:
                        case TestHeap:
                        case Jump:
                        case SelectValue:
                        case SelectTupleArity:
                            break;
                        case UpdateRecord when allowRegisterRecordUpdates
                            && instruction.Operands[3].Kind == BeamOperandParameters.X:
                            break;
                        case PutTuple:
                        case PutList:
                            if (requireClearedSlot && !initialized
                                && SameProjectionRegister(instruction.Operands[StackWriteOperand(instruction)], slot))
                            {
                                return false;
                            }

                            break;
                        default:
                            if (!IsProjectionFlowTest(instruction.Definition.Opcode)
                                && !(allowFramePredicates && IsEntryFramePredicate(instruction.Definition.Opcode)))
                            {
                                return false;
                            }

                            break;
                    }

                    work.Push((previous, initialized));
                }
            }

            return foundFrame;
        }

        internal bool IsDead(
            int continuation,
            BeamOperand slot,
            bool allowBadRecordExit = false,
            bool afterCall = false,
            int dictionaryLookupImport = -1,
            bool allowEntryFramePredicates = false,
            bool endsAtCatchSlotWrite = false
        )
        {
            var work = new Stack<(int Index, BeamOperand Slot, bool PassedCall)>();
            var seen = new HashSet<(int Index, BeamOperand Slot, bool PassedCall)>();
            bool foundEnd = false;
            work.Push((continuation, slot, afterCall));
            while (work.TryPop(out var state))
            {
                if (!seen.Add(state))
                {
                    continue;
                }

                var instruction = body[state.Index];
                bool passedCall = state.PassedCall;
                BeamOperand current = state.Slot;
                switch (instruction.Definition.Opcode)
                {
                    case LocalCall:
                    case ExternalCall:
                        passedCall = true;
                        break;
                    case TailLocalCallWithFrame:
                    case TailExternalCallWithFrame:
                        foundEnd = true;
                        continue;
                    case Deallocate:
                    case CaseEnd:
                    case BadMatch:
                    case BadRecord when allowBadRecordExit:
                        if (!passedCall || instruction.Operands.Any(operand => SameProjectionRegister(operand, current)))
                        {
                            return false;
                        }

                        foundEnd = true;
                        continue;
                    case Move:
                    case GetTupleElement:
                        if (SameProjectionRegister(instruction.Operands[0], current))
                        {
                            return false;
                        }

                        if (SameProjectionRegister(instruction.Operands[^1], current))
                        {
                            if (!passedCall)
                            {
                                return false;
                            }

                            foundEnd = true;
                            continue;
                        }

                        break;
                    case InitYRegisters:
                        if (instruction.Operands.Any(operand => SameProjectionRegister(operand, current)))
                        {
                            if (!passedCall)
                            {
                                return false;
                            }

                            foundEnd = true;
                            continue;
                        }

                        break;
                    case TrimFrame:
                        var removed = instruction.Operands[0].Value;
                        if (current.Value < removed)
                        {
                            if (!passedCall)
                            {
                                return false;
                            }

                            foundEnd = true;
                            continue;
                        }

                        current = current with { Value = current.Value - removed };
                        break;
                    case CatchEnter when endsAtCatchSlotWrite && passedCall
                        && SameProjectionRegister(instruction.Operands[0], current):
                        foundEnd = true;
                        continue;
                    case BifOne when IsInfallibleDictionaryLookup(instruction, dictionaryLookupImport):
                        break;
                    case Jump:
                    case Label:
                    case Line:
                    case TestHeap:
                        break;
                    default:
                        byte opcode = instruction.Definition.Opcode;
                        if (!(opcode is PutTuple or PutList or SelectValue or SelectTupleArity || IsProjectionFlowTest(opcode)
                                || allowEntryFramePredicates && IsEntryFramePredicate(opcode))
                            || instruction.Operands.Any(operand => SameProjectionRegister(operand, current)))
                        {
                            return false;
                        }

                        break;
                }

                if (successors[state.Index].Length == 0)
                {
                    return false;
                }

                foreach (int target in successors[state.Index])
                {
                    work.Push((target, current, passedCall));
                }
            }

            return foundEnd;
        }

        internal bool IsEntryTemporaryDead(int entry, BeamOperand temporary) =>
            IsTemporaryDead([entry], temporary, preserveHeapRoots: true);

        internal bool IsBranchTemporaryDead(int branch, BeamOperand temporary) =>
            IsTemporaryDead(successors[branch], temporary, preserveHeapRoots: true);

        internal bool IsGuardTemporaryDeadOnFailure(int guard, BeamOperand temporary, bool preserveHeapRoots) =>
            successors[guard] is [int failure, int success] && success == guard + 1
                && failure != success && IsTemporaryDead([failure], temporary, preserveHeapRoots);

        internal bool IsFallthroughTemporaryDead(int index, BeamOperand register, bool trackUnprotectedBifs = false) =>
            successors[index].Contains(index + 1)
            && IsTemporaryDead(
                [index + 1],
                register,
                preserveHeapRoots: true,
                trackReceive: true,
                trackUnprotectedBifs: trackUnprotectedBifs
            );

        internal bool IsAllocationRootDead(int allocation, BeamOperand register) =>
            IsTemporaryDead(
                successors[allocation],
                register,
                preserveHeapRoots: true,
                trackReceive: true
            );

        private bool IsTemporaryDead(
            IEnumerable<int> entries,
            BeamOperand temporary,
            bool preserveHeapRoots,
            bool trackReceive = false,
            bool trackUnprotectedBifs = false
        )
        {
            var visited = new HashSet<int>();
            var pending = new Stack<int>(entries);
            while (pending.TryPop(out int index))
            {
                if (!visited.Add(index))
                {
                    continue;
                }

                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (trackReceive && opcode == LoopReceive)
                {
                    // loop_rec writes the message register only when a message was found.
                    if (SameProjectionRegister(instruction.Operands[1], temporary))
                    {
                        if (successors[index] is not [int failure, int success] || success != index + 1 || failure == success)
                        {
                            return false;
                        }

                        pending.Push(failure);
                    }
                    else
                    {
                        foreach (int target in successors[index])
                        {
                            pending.Push(target);
                        }
                    }

                    continue;
                }

                bool unprotectedBif = trackUnprotectedBifs && opcode is (GcBifOne or GcBifTwo)
                    && instruction.Operands[0].Value.IsZero;
                int implicitInputs = opcode switch
                {
                    LocalCall or ExternalCall or TailLocalCall or TailLocalCallWithFrame
                        or TailExternalCall or TailExternalCallWithFrame => (int)instruction.Operands[0].Value,
                    DynamicApply or TailDynamicApply => (int)instruction.Operands[0].Value + 2,
                    CallFun => (int)instruction.Operands[0].Value + 1,
                    Allocate or AllocateHeap or FunctionInfo => (int)instruction.Operands[^1].Value,
                    TestHeap when preserveHeapRoots => (int)instruction.Operands[^1].Value,
                    GcBifOne or GcBifTwo when unprotectedBif => (int)instruction.Operands[1].Value,
                    Return => 1,
                    _ => 0
                };
                if (temporary.Value < implicitInputs)
                {
                    return false;
                }

                if (opcode is LocalCall or ExternalCall or TailLocalCall or TailLocalCallWithFrame
                    or TailExternalCall or TailExternalCallWithFrame or DynamicApply or TailDynamicApply
                    or CallFun or FunctionInfo or Return)
                {
                    continue;
                }

                int destination = opcode switch
                {
                    Move or GetTupleElement or GetHead or GetTail or GetList or PutList or PutTuple =>
                        StackWriteOperand(instruction),
                    GcBifOne or GcBifTwo when unprotectedBif => instruction.Operands.Count - 1,
                    _ => -1
                };
                bool overwritten = false;
                for (int position = 0; position < instruction.Operands.Count; position++)
                {
                    if (!SameProjectionRegister(instruction.Operands[position], temporary))
                    {
                        continue;
                    }

                    if (position != destination && !(opcode == GetList && position == 2))
                    {
                        return false;
                    }

                    overwritten = true;
                }

                if (overwritten || opcode is CaseEnd or BadMatch or BadRecord or IfEnd)
                {
                    continue;
                }

                if (destination < 0 && !(IsEntryFramePredicate(opcode) || opcode is Label or Line or Jump
                    or SelectValue or SelectTupleArity or Allocate or AllocateHeap or Deallocate
                    or InitYRegisters or TrimFrame or TestHeap)
                    && !(trackReceive && opcode is (RemoveMessage or ReceiveTimeout or LoopReceiveEnd or Wait or WaitTimeout)))
                {
                    return false;
                }

                foreach (int target in successors[index])
                {
                    pending.Push(target);
                }
            }

            return true;
        }

        private static bool FallsThrough(byte opcode) => opcode is not (
            Jump or SelectValue or SelectTupleArity or EndCode or Return
            or TailLocalCall or TailExternalCall or TailLocalCallWithFrame or TailExternalCallWithFrame
            or TailDynamicApply or BadMatch or CaseEnd or IfEnd or TryCaseEnd
        );

        private static bool IsProjectionFlowTest(byte opcode) =>
            opcode is IsTuple or TestArity or IsTaggedTuple or ExactEqualTest or ExactNotEqualTest
                or IsNil or IsNonemptyList;
    }
}
