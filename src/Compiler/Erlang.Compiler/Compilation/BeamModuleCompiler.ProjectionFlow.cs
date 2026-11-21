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
            bool allowFramePredicates = false
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
                            initialized |= SameProjectionRegister(instruction.Operands[^1], slot);
                            break;
                        case GetList:
                            initialized |= SameProjectionRegister(instruction.Operands[1], slot)
                                || SameProjectionRegister(instruction.Operands[2], slot);
                            break;
                        case CatchEnter when previous == closedCatchEnter
                            && !SameProjectionRegister(instruction.Operands[0], slot):
                        case CatchEnd when previous == closedCatchEnd
                            && !SameProjectionRegister(instruction.Operands[0], slot):
                        case LocalCall:
                        case ExternalCall:
                        case Label:
                        case Line:
                        case TestHeap:
                        case PutTuple:
                        case PutList:
                        case Jump:
                        case SelectValue:
                        case SelectTupleArity:
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
            bool afterCall = false
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
                    case Jump:
                    case Label:
                    case Line:
                    case TestHeap:
                        break;
                    default:
                        byte opcode = instruction.Definition.Opcode;
                        if (!(opcode is PutTuple or PutList or SelectValue or SelectTupleArity || IsProjectionFlowTest(opcode))
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
            bool trackReceive = false
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

                int implicitInputs = opcode switch
                {
                    LocalCall or ExternalCall or TailLocalCall or TailLocalCallWithFrame
                        or TailExternalCall or TailExternalCallWithFrame => (int)instruction.Operands[0].Value,
                    DynamicApply or TailDynamicApply => (int)instruction.Operands[0].Value + 2,
                    CallFun => (int)instruction.Operands[0].Value + 1,
                    Allocate or AllocateHeap or FunctionInfo => (int)instruction.Operands[^1].Value,
                    TestHeap when preserveHeapRoots => (int)instruction.Operands[^1].Value,
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
