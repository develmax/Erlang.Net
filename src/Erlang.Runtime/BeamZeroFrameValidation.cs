namespace Erlang;

internal static class BeamZeroFrameValidation
{
    public static void Validate(BeamLinkedCode code, BeamLinkedFunction function, IReadOnlyDictionary<int, BeamReceivePlan> receives)
    {
        var work = new Stack<(int Pc, int Depth)>();
        var seen = new Dictionary<int, int>();
        work.Push((function.FirstInstructionIndex, 0));

        void Next(int pc, int depth)
        {
            if (pc < function.FirstInstructionIndex || pc >= function.EndInstructionIndex)
                throw new InvalidDataException(BeamReceiveNames.InvalidFrame);
            work.Push((pc, depth));
        }
        while (work.TryPop(out var current))
        {
            if (seen.TryGetValue(current.Pc, out int prior))
            {
                if (prior != current.Depth)
                    throw new NotSupportedException(BeamReceiveNames.InvalidFrame);
                continue;
            }
            seen.Add(current.Pc, current.Depth);
            var instruction = code.Instructions[current.Pc];
            int depth = current.Depth;
            switch (instruction.Definition.Name)
            {
                case BeamLinkNames.Allocate:
                    Next(current.Pc + 1, depth + 1);
                    break;
                case BeamLinkNames.Deallocate:
                    if (depth == 0)
                        throw new InvalidDataException(BeamReceiveNames.InvalidFrame);
                    Next(current.Pc + 1, depth - 1);
                    break;
                case BeamLinkNames.Return:
                case BeamLinkNames.LocalCall:
                case BeamLinkNames.ExternalTailCall:
                    if (depth != 0)
                        throw new InvalidDataException(BeamReceiveNames.InvalidFrame);
                    break;
                case BeamLinkNames.LoopReceive:
                    BeamReceivePlan plan = receives[current.Pc];
                    foreach (int node in plan.MatcherInstructions)
                        if (code.Instructions[node].Definition.Name == BeamLinkNames.RemoveMessage)
                            Next(node + 1, depth);
                    Next(plan.TimeoutInstructionIndex + 1, depth);
                    break;
                case BeamLinkNames.IntegerTest:
                case BeamLinkNames.EqualityTest:
                case BeamLinkNames.GuardBif:
                    BeamOperand fail = instruction.Operands[0];
                    if (fail.Kind == BeamOperandParameters.Label && fail.Value != function.InfoLabel)
                        Next(fail.Value == function.EntryLabel ? function.FirstInstructionIndex : code.Labels[(int)fail.Value], depth);
                    Next(current.Pc + 1, depth);
                    break;
                default:
                    Next(current.Pc + 1, depth);
                    break;
            }
        }
    }
}
