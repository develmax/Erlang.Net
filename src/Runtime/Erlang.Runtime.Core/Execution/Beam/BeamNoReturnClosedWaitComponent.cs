namespace Erlang;

internal static class BeamNoReturnClosedWaitComponent
{
    internal static bool Prove(BeamLinkedCode code, int entryLabel)
    {
        // Conservative lexical over-approximation: inspect every instruction,
        // including unreachable arms. Return/handlers/unknown tails refuse.
        // Closed local tail edges may cycle; cycles cannot return by themselves.
        var pending = new Stack<int>();
        var seen = new HashSet<int>();
        bool hasWait = false;
        pending.Push(entryLabel);
        while (pending.TryPop(out int label))
        {
            if (!seen.Add(label))
                continue;
            BeamLinkedFunction? function = code.Functions.SingleOrDefault(value => value.EntryLabel == label);
            if (function is null)
                return false;
            bool terminal = false;
            for (int pc = function.FirstInstructionIndex; pc < function.EndInstructionIndex; pc++)
            {
                BeamInstruction instruction = code.Instructions[pc];
                string name = instruction.Definition.Name;
                var operands = instruction.Operands;
                if (name is BeamLinkNames.Label or BeamLinkNames.Line)
                    continue;
                terminal = false;
                if (name is BeamLinkNames.LocalCall or BeamGeneralOpcodeNames.LocalLast)
                {
                    BeamLinkedFunction? callee = code.Functions.SingleOrDefault(value => value.EntryLabel == operands[1].Value
                        && value.Arity == operands[0].Value);
                    if (callee is null)
                        return false;
                    pending.Push(callee.EntryLabel);
                    terminal = true;
                    continue;
                }
                if (name is BeamLinkNames.ExternalTailCall or BeamOnLoadNames.ExternalLast)
                {
                    if (!BeamNoReturnAnalysis.Import(code, (int)operands[1].Value))
                        return false;
                    terminal = true;
                    continue;
                }
                if (name == BeamRegisterNames.Call)
                {
                    // A returning ordinary callee resumes here; every possible
                    // continuation is still inside this no-return function.
                    if (!code.Functions.Any(value => value.EntryLabel == operands[1].Value
                        && value.Arity == operands[0].Value))
                        return false;
                    continue;
                }
                if (name == BeamLinkNames.ExternalCall)
                {
                    terminal = BeamNoReturnAnalysis.Import(code, (int)operands[1].Value);
                    continue;
                }
                if (name is BeamOnLoadNames.BadMatch or BeamGeneralOpcodeNames.CaseEnd)
                {
                    terminal = true;
                    continue;
                }
                if (name is BeamGeneralOpcodeNames.Jump or BeamGeneralOpcodeNames.SelectValue
                    or BeamGeneralOpcodeNames.SelectTupleArity
                    or BeamLinkNames.Wait or BeamLinkNames.LoopReceiveEnd)
                {
                    terminal = true;
                    hasWait |= name == BeamLinkNames.Wait;
                }
                else if (name is BeamLinkNames.Allocate
                or BeamGeneralOpcodeNames.BinaryBif
                or BeamBinaryNames.Create
                or BeamBinaryNames.ExtractBinary
                or BeamBinaryNames.SavePosition
                or BeamBinaryNames.ExtractTail
                or BeamBinaryNames.Match
                or BeamBinaryNames.RestorePosition
                or BeamBinaryNames.Start
                or BeamGeneralOpcodeNames.UnaryGcBif
                or BeamLinkNames.GuardBif
                or BeamGeneralOpcodeNames.GetList
                or BeamOnLoadNames.TupleElement
                or BeamOnLoadNames.InitY
                or BeamGeneralOpcodeNames.ArithmeticEqual
                or BeamLinkNames.EqualityTest
                or BeamGeneralOpcodeNames.GreaterOrEqual
                or BeamGeneralOpcodeNames.Less
                or BeamGeneralOpcodeNames.ExactNotEqual
                or BeamGeneralOpcodeNames.NilTest
                or BeamGeneralOpcodeNames.NonemptyListTest
                or BeamOnLoadNames.TaggedTupleTest
                or BeamGeneralOpcodeNames.TupleTest
                or BeamLinkNames.LoopReceive
                or BeamLinkNames.Move
                or BeamLambdaNames.Create
                or BeamCollectionOpcodeNames.UpdateRecord
                or BeamLinkNames.PutTuple
                or BeamLinkNames.PutList
                or BeamLinkNames.RemoveMessage
                or BeamGeneralOpcodeNames.Swap
                or BeamGeneralOpcodeNames.TupleArity
                or BeamLinkNames.HeapTest
                or BeamLinkNames.Timeout
                or BeamGeneralOpcodeNames.Trim
                or BeamLinkNames.WaitTimeout)
                {
                    hasWait |= name == BeamLinkNames.WaitTimeout;
                }
                else
                    return false;
                // Non-call labels must remain in this exact function, or its
                // InfoLabel failure entry. Actual typed ownership stays with
                // the normal register/frame/receive validators.
                foreach (BeamOperand operand in operands)
                    if (operand.Kind == BeamOperandParameters.Label && operand.Value != function.InfoLabel
                        && operand.Value != function.EntryLabel
                        && (!code.Labels.TryGetValue((int)operand.Value, out int target)
                            || target < function.FirstInstructionIndex || target >= function.EndInstructionIndex))
                        return false;
            }
            if (!terminal)
                return false;
        }

        // Deliberately leave pure-cycle-only admission at its prior policy.
        // Presence is a scope limit, not a claim of progress or termination.
        return hasWait;
    }
}
