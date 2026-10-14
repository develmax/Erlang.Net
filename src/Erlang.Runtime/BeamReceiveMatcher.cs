namespace Erlang;

internal static class BeamReceiveMatcher
{
    public static BeamReceiveSelection? Select(
        BeamLinkedCode code,
        BeamReceivePlan plan,
        BeamRegisterState before,
        Term message
    )
    {
        BeamRegisterState registers = before.Clone();
        registers.Write(plan.MessageRegister, message);
        int pc = plan.ScanInstructionIndex + 1;
        while (plan.MatcherInstructions.Contains(pc))
        {
            var instruction = code.Instructions[pc++];
            var operands = instruction.Operands;
            switch (instruction.Definition.Name)
            {
                case BeamLinkNames.Label:
                case BeamLinkNames.Line:
                    break;
                case BeamLinkNames.Move:
                    registers.Write(operands[1], Read(code, registers, operands[0]));
                    break;
                case BeamOnLoadNames.PidTest:
                    if (Read(code, registers, operands[1]) is not Pid)
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamOnLoadNames.TaggedTupleTest:
                    if (Read(code, registers, operands[1]) is not TupleTerm tuple
                        || tuple.Items.Count != operands[2].Value
                        || tuple.Items.Count == 0
                        || !tuple.Items[0].Equals(Read(code, registers, operands[3])))
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamOnLoadNames.TupleElement:
                    registers.Write(operands[2], ((TupleTerm)Read(code, registers, operands[0])).Items[(int)operands[1].Value]);
                    break;
                case BeamLinkNames.IntegerTest:
                    if (Read(code, registers, operands[1]) is not Integer)
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamLinkNames.EqualityTest:
                    if (!Read(code, registers, operands[1]).Equals(Read(code, registers, operands[2])))
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamLinkNames.RemoveMessage:
                    return new(pc, registers);
                case BeamLinkNames.LoopReceiveEnd:
                    return null;
                default:
                    throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
            }
        }

        throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
    }

    private static Term Read(BeamLinkedCode code, BeamRegisterState registers, BeamOperand operand) => operand.Kind switch
    {
        BeamOperandParameters.X => registers.Read((int)operand.Value),
        BeamOperandParameters.Y => registers.ReadY((int)operand.Value),
        BeamOperandParameters.Integer => new Integer(operand.Value),
        BeamOperandParameters.Atom => Term.A(code.Structure.Atoms[(int)operand.Value]!),
        BeamOperandParameters.Nil => Nil.Value,
        BeamOperandParameters.LiteralValue => code.Structure.Literals[(int)operand.Value],
        _ => throw new InvalidDataException(BeamReceiveNames.InvalidPlan)
    };
}
