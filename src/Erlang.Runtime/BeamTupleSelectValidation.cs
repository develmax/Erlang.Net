namespace Erlang;

internal static class BeamTupleSelectValidation
{
    internal static void Validate(BeamLinkedCode code, BeamLinkedFunction function)
    {
        for (int pc = function.FirstInstructionIndex; pc < function.EndInstructionIndex; pc++)
        {
            BeamInstruction instruction = code.Instructions[pc];
            if (instruction.Definition.Name != BeamGeneralOpcodeNames.SelectTupleArity)
                continue;
            // The original compiler omits a redundant guard for typed private entries.
            // The interpreter still checks the actual value before reading tuple size.
            if (instruction.Operands[0].TypeIndex is int typeIndex && !code.Types.Fallback
                && code.Types.Entries[typeIndex].TypeUnion == BeamTypeWire.Tuple)
                continue;
            int proof = pc - 1;
            while (proof >= function.FirstInstructionIndex && code.Instructions[proof].Definition.Name == BeamLinkNames.Line)
                proof--;
            // No label or intervening operation can enter past this successful tuple guard.
            if (proof < function.FirstInstructionIndex
                || code.Instructions[proof].Definition.Name != BeamGeneralOpcodeNames.TupleTest
                || code.Instructions[proof].Operands[1].Kind != instruction.Operands[0].Kind
                || code.Instructions[proof].Operands[1].Value != instruction.Operands[0].Value)
                throw new NotSupportedException(BeamLinkDiagnostics.OpcodeRole);
        }
    }

    internal static BeamOperand Select(Term value, IReadOnlyList<BeamOperand> operands)
    {
        if (value is not TupleTerm tuple)
            return operands[1];
        for (int index = 3; index < operands.Count; index += 2)
            if (tuple.Items.Count == operands[index].Value)
                return operands[index + 1];

        return operands[1];
    }
}
