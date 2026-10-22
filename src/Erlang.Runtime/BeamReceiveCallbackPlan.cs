namespace Erlang;

internal sealed record BeamReceiveCallbackPlan(BeamOperand Callee, int CallInstructionIndex, int SuccessInstructionIndex)
{
    internal static (BeamReceiveCallbackPlan Callback, IReadOnlySet<int> Instructions)? Read(
        BeamLinkedCode code,
        BeamLinkedFunction function,
        int scan
    )
    {
        if (code.Instructions[scan].Operands[1].Value != 0)
            return null;
        int first = scan + 1;
        if (first + 3 >= function.EndInstructionIndex)
            return null;
        BeamInstruction move = code.Instructions[first];
        BeamInstruction call = code.Instructions[first + 1];
        BeamInstruction test = code.Instructions[first + 2];
        BeamInstruction remove = code.Instructions[first + 3];
        if (move.Definition.Name != BeamLinkNames.Move || move.Operands[0].Kind != BeamOperandParameters.Y
            || move.Operands[1].Kind != BeamOperandParameters.X || move.Operands[1].Value != 1
            || call.Definition.Name != BeamGeneralOpcodeNames.DynamicCall || call.Operands[0].Value != 1
            || test.Definition.Name != BeamGeneralOpcodeNames.ExactNotEqual
            || test.Operands[0].Kind != BeamOperandParameters.Label || test.Operands[0].Value == 0
            || test.Operands[1].Kind != BeamOperandParameters.X || test.Operands[1].Value != 0
            || test.Operands[2].Kind != BeamOperandParameters.Atom
            || code.Structure.Atoms[(int)test.Operands[2].Value] != BeamReceiveCallbackNames.NoMatch
            || remove.Definition.Name != BeamLinkNames.RemoveMessage)
            return null;
        int reject = code.Labels[(int)test.Operands[0].Value];
        if (reject < function.FirstInstructionIndex)
            return null;
        var instructions = new HashSet<int>(Enumerable.Range(first, 4));
        while (reject < function.EndInstructionIndex && code.Instructions[reject].Definition.Name is BeamLinkNames.Label or BeamLinkNames.Line)
            instructions.Add(reject++);
        if (reject >= function.EndInstructionIndex || code.Instructions[reject].Definition.Name != BeamLinkNames.LoopReceiveEnd)
            return null;
        int target = code.Labels[(int)code.Instructions[reject].Operands[0].Value];
        while (target < function.EndInstructionIndex && code.Instructions[target].Definition.Name is BeamLinkNames.Label or BeamLinkNames.Line)
            target++;
        if (target != scan)
            return null;
        instructions.Add(reject);

        return (new(move.Operands[0], first + 1, first + 4), instructions);
    }
}
