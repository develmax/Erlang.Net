namespace Erlang;

internal static class BeamReceivePlanner
{
    public static BeamReceivePlan Build(BeamLinkedCode code, BeamLinkedFunction function, int scan)
    {
        var loop = code.Instructions[scan];
        if (loop.Operands[1].Kind != BeamOperandParameters.X)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
        int wait = SkipMetadata(code, Label(code, loop.Operands[0]));
        if (wait < function.FirstInstructionIndex || wait >= function.EndInstructionIndex
            || code.Instructions[wait].Definition.Name != BeamLinkNames.WaitTimeout)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
        var waiting = code.Instructions[wait];
        if (SkipMetadata(code, Label(code, waiting.Operands[0])) != scan)
            throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
        BeamOperand timeout = waiting.Operands[1];
        TimeSpan? duration = timeout.Kind switch
        {
            BeamOperandParameters.Integer when timeout.Value >= 0 && timeout.Value <= BeamReceiveNames.MaximumMilliseconds => TimeSpan.FromMilliseconds((double)timeout.Value),
            BeamOperandParameters.Atom when code.Structure.Atoms[(int)timeout.Value] == BeamReceiveNames.Infinity => null,
            _ => throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher)
        };
        int expired = SkipMetadata(code, wait + 1);
        if (expired >= function.EndInstructionIndex || code.Instructions[expired].Definition.Name != BeamLinkNames.Timeout)
            throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
        var seen = new HashSet<int>();
        var active = new HashSet<int>();
        bool success = false;
        bool rejection = false;

        void Visit(int pc)
        {
            if (pc < function.FirstInstructionIndex || pc >= function.EndInstructionIndex || pc == scan || active.Contains(pc))
                throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
            if (!seen.Add(pc))
                return;
            active.Add(pc);
            var instruction = code.Instructions[pc];
            var operands = instruction.Operands;
            switch (instruction.Definition.Name)
            {
                case BeamLinkNames.RemoveMessage:
                    success = true;
                    break;
                case BeamLinkNames.LoopReceiveEnd:
                    if (SkipMetadata(code, Label(code, operands[0])) != scan)
                        throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
                    rejection = true;
                    break;
                case BeamLinkNames.Label:
                case BeamLinkNames.Line:
                    Visit(pc + 1);
                    break;
                case BeamLinkNames.Move:
                    if (operands[1].Kind != BeamOperandParameters.X)
                        throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                    Source(operands[0]);
                    Visit(pc + 1);
                    break;
                case BeamLinkNames.IntegerTest:
                    Source(operands[1]);
                    Visit(Label(code, operands[0]));
                    Visit(pc + 1);
                    break;
                case BeamLinkNames.EqualityTest:
                    Source(operands[1]);
                    Source(operands[2]);
                    Visit(Label(code, operands[0]));
                    Visit(pc + 1);
                    break;
                default:
                    throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
            }
            active.Remove(pc);
        }
        Visit(scan + 1);
        if (!success || !rejection)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);

        return new(
            scan,
            (int)loop.Operands[1].Value,
            wait,
            expired,
            duration,
            seen
        );
    }

    private static int Label(BeamLinkedCode code, BeamOperand operand)
    {
        if (operand.Kind != BeamOperandParameters.Label)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);

        return code.Labels[(int)operand.Value];
    }

    private static int SkipMetadata(BeamLinkedCode code, int pc)
    {
        while (pc < code.Instructions.Count && code.Instructions[pc].Definition.Name is BeamLinkNames.Label or BeamLinkNames.Line)
            pc++;

        return pc;
    }

    private static void Source(BeamOperand operand)
    {
        if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Integer or BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue))
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
    }
}
