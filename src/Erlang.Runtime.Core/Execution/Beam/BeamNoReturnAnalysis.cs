namespace Erlang;

internal static class BeamNoReturnAnalysis
{
    internal static bool Import(BeamLinkedCode code, int index)
    {
        BeamImport import = code.Structure.Imports[index];
        if (code.Structure.Atoms[import.ModuleAtomIndex] != ErlangModuleNames.Module)
            return false;
        string? name = code.Structure.Atoms[import.FunctionAtomIndex];

        return (name is ErlangModuleNames.Throw or ErlangModuleNames.Exit) && import.Arity == 1
            || name == ErlangModuleNames.Error && import.Arity is 1 or 2 or 3;
    }

    internal static bool Local(BeamLinkedCode code, int entryLabel)
        => Prove(code, entryLabel, ordinaryCallContinuation: false)
            || Prove(code, entryLabel, ordinaryCallContinuation: true)
            || BeamNoReturnClosedWaitComponent.Prove(code, entryLabel);

    private static bool Prove(BeamLinkedCode code, int entryLabel, bool ordinaryCallContinuation)
    {
        // Preserve strict-prefix proof and separately prove a caller's continuation.
        // An ordinary callee may return, throw or diverge: if its continuation cannot
        // return either, the caller cannot return. Tail callees still need proof.
        // Unknown operations, handlers, returns and cycles remain unproven.
        BeamLinkedFunction entry = code.Functions.Single(function => function.EntryLabel == entryLabel);
        var proven = new HashSet<int>();
        var active = new HashSet<int>();
        var work = new Stack<(BeamLinkedFunction Function, int Pc, bool Finish)>();
        work.Push((entry, entry.FirstInstructionIndex, false));
        while (work.TryPop(out var current))
        {
            if (proven.Contains(current.Pc))
                continue;
            if (current.Finish)
            {
                active.Remove(current.Pc);
                proven.Add(current.Pc);
                continue;
            }
            BeamLinkedFunction function = current.Function;
            if (current.Pc < function.FirstInstructionIndex || current.Pc >= function.EndInstructionIndex
                || !active.Add(current.Pc))
                return false;
            BeamInstruction instruction = code.Instructions[current.Pc];
            var operands = instruction.Operands;
            var successors = new List<(BeamLinkedFunction Function, int Pc)>();

            bool Target(BeamOperand operand)
            {
                if (operand.Kind != BeamOperandParameters.Label)
                    return false;
                if (operand.Value == function.InfoLabel)
                    return true;
                int pc = operand.Value == function.EntryLabel ? function.FirstInstructionIndex : code.Labels[(int)operand.Value];
                if (pc < function.FirstInstructionIndex || pc >= function.EndInstructionIndex)
                    return false;
                successors.Add((function, pc));

                return true;
            }
            switch (instruction.Definition.Name)
            {
                case BeamCollectionOpcodeNames.BadRecord:
                case BeamGeneralOpcodeNames.IfEnd:
                    break;
                case BeamLinkNames.Label:
                case BeamLinkNames.Line:
                case BeamLinkNames.Move:
                case BeamGeneralOpcodeNames.Swap:
                case BeamLinkNames.Allocate:
                case BeamLinkNames.AllocateHeap:
                case BeamLinkNames.HeapTest:
                case BeamOnLoadNames.InitY:
                // Compiled receive-after-zero resumes immediately; timeout
                // resets receive state and falls through without returning.
                case BeamLinkNames.Timeout:
                case BeamLinkNames.PutTuple:
                case BeamLinkNames.PutList:
                case BeamCollectionOpcodeNames.UpdateRecord:
                case BeamOnLoadNames.TupleElement:
                case BeamGeneralOpcodeNames.Trim:
                    successors.Add((function, current.Pc + 1));
                    break;
                case BeamLinkNames.ExternalCall:
                    if (!Import(code, (int)operands[1].Value))
                    {
                        if (!ordinaryCallContinuation)
                            return false;
                        successors.Add((function, current.Pc + 1));
                    }
                    break;
                case BeamLinkNames.ExternalTailCall:
                case BeamOnLoadNames.ExternalLast:
                    if (!Import(code, (int)operands[1].Value))
                        return false;
                    break;
                case BeamRegisterNames.Call:
                    if (ordinaryCallContinuation)
                    {
                        BeamLinkedFunction? continuationCallee = code.Functions.SingleOrDefault(target => target.EntryLabel == operands[1].Value);
                        if (continuationCallee is null || continuationCallee.Arity != operands[0].Value)
                            return false;
                        successors.Add((function, current.Pc + 1));
                        break;
                    }
                    goto case BeamLinkNames.LocalCall;
                case BeamLinkNames.LocalCall:
                case BeamGeneralOpcodeNames.LocalLast:
                    BeamLinkedFunction? callee = code.Functions.SingleOrDefault(target => target.EntryLabel == operands[1].Value);
                    if (callee is null || callee.Arity != operands[0].Value)
                        return false;
                    successors.Add((callee, callee.FirstInstructionIndex));
                    break;
                case BeamGeneralOpcodeNames.Jump:
                    if (!Target(operands[0]))
                        return false;
                    break;
                case BeamGeneralOpcodeNames.SelectValue:
                case BeamGeneralOpcodeNames.SelectTupleArity:
                    if (!Target(operands[1]))
                        return false;
                    for (int target = 4; target < operands.Count; target += 2)
                        if (!Target(operands[target]))
                            return false;
                    break;
                default:
                    return false;
            }
            work.Push((function, current.Pc, true));
            foreach (var next in successors)
                work.Push((next.Function, next.Pc, false));
        }

        return true;
    }
}
