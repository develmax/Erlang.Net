using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed class BeamProjectionFlow
    {
        private readonly IReadOnlyList<BeamInstruction> body;
        private readonly List<int>[] predecessors;
        private readonly int[][] successors;

        internal BeamProjectionFlow(IReadOnlyList<BeamInstruction> body)
        {
            this.body = body;
            var labels = body.Select((instruction, index) => (instruction, index))
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

        internal bool CanEliminate(int projection, int continuation, BeamOperand slot) =>
            IsDead(continuation, slot) && IsInitialized(projection, slot);

        private bool IsInitialized(int projection, BeamOperand slot)
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
                            initialized |= SameProjectionRegister(instruction.Operands[^1], slot);
                            break;
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
                            if (!IsProjectionFlowTest(instruction.Definition.Opcode))
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

        private bool IsDead(int continuation, BeamOperand slot)
        {
            var work = new Stack<(int Index, BeamOperand Slot, bool PassedCall)>();
            var seen = new HashSet<(int Index, BeamOperand Slot, bool PassedCall)>();
            bool foundEnd = false;
            work.Push((continuation, slot, false));
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

        private static bool FallsThrough(byte opcode) => opcode is not (
            Jump or SelectValue or SelectTupleArity or EndCode or Return
            or TailLocalCall or TailExternalCall or TailLocalCallWithFrame or TailExternalCallWithFrame
            or TailDynamicApply or BadMatch or CaseEnd or TryCaseEnd
        );

        private static bool IsProjectionFlowTest(byte opcode) =>
            opcode is IsTuple or TestArity or IsTaggedTuple or ExactEqualTest or ExactNotEqualTest
                or IsNil or IsNonemptyList;
    }
}
