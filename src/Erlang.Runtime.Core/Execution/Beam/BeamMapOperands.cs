namespace Erlang;

internal static class BeamMapOperands
{
    internal static void ValidateKeys(
        BeamStructure structure,
        IReadOnlyList<BeamOperand> operands,
        int start,
        int stride
    )
    {
        int count = (operands.Count - start) / stride;
        if (count == 0)
            throw new InvalidDataException(BeamLinkDiagnostics.OperandRole);
        if (count == 1)
            return;
        var keys = new HashSet<Term>();
        for (int index = start; index < operands.Count; index += stride)
        {
            BeamOperand operand = operands[index];
            Term key = operand.Kind switch
            {
                BeamOperandParameters.Atom => Term.A(structure.Atoms[(int)operand.Value]!),
                BeamOperandParameters.Nil => Nil.Value,
                BeamOperandParameters.Integer => new Integer(operand.Value),
                BeamOperandParameters.LiteralValue => structure.Literals[(int)operand.Value],
                _ => throw new InvalidDataException(BeamLinkDiagnostics.OperandRole)
            };
            if (!keys.Add(key))
                throw new InvalidDataException(BeamLinkDiagnostics.OperandRole);
        }
    }
}
