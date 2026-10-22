namespace Erlang;

internal static class BeamSelectValueValidation
{
    internal static void Validate(BeamStructure structure, IReadOnlyList<BeamOperand> operands)
    {
        Type? kind = null;
        var choices = new HashSet<Term>();
        for (int index = 3; index < operands.Count; index += 2)
        {
            BeamOperand operand = operands[index];
            Term choice = operand.Kind switch
            {
                BeamOperandParameters.Atom => Term.A(structure.Atoms[(int)operand.Value]!),
                BeamOperandParameters.Integer => new Integer(operand.Value),
                BeamOperandParameters.LiteralValue => structure.Literals[(int)operand.Value],
                _ => throw new InvalidDataException(BeamLinkDiagnostics.OperandRole)
            };
            if (choice is not (Atom or Integer or FloatTerm) || kind is not null && kind != choice.GetType() || !choices.Add(choice))
                throw new InvalidDataException(BeamLinkDiagnostics.OperandRole);
            kind = choice.GetType();
        }
    }
}
