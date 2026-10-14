using System.Numerics;
namespace Erlang;

internal static class BeamFrameValidation
{
    internal static void Validate(BeamLinkedCode code, BeamLinkedFunction function, IReadOnlyDictionary<int, BeamReceivePlan> receives)
    {
        var work = new Stack<(int Pc, int Slots, BigInteger Initialized)>();
        var seen = new Dictionary<int, (int Slots, BigInteger Initialized)>();
        work.Push((function.FirstInstructionIndex, BeamFrameParameters.NoFrame, BigInteger.Zero));

        void Next(int pc, int slots, BigInteger initialized)
        {
            if (pc < function.FirstInstructionIndex || pc >= function.EndInstructionIndex)
                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            work.Push((pc, slots, initialized));
        }
        while (work.TryPop(out var current))
        {
            int slots = current.Slots;
            BigInteger initialized = current.Initialized;
            if (seen.TryGetValue(current.Pc, out var previous))
            {
                if (slots != previous.Slots)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                initialized &= previous.Initialized;
                if (initialized == previous.Initialized)
                    continue;
            }
            seen[current.Pc] = (slots, initialized);
            BeamInstruction instruction = code.Instructions[current.Pc];
            var operands = instruction.Operands;

            void Y(BeamOperand operand, bool source)
            {
                if (operand.Kind != BeamOperandParameters.Y)
                    return;
                int index = (int)operand.Value;
                if (index < 0 || index >= slots || source && (initialized & (BigInteger.One << index)) == 0)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            }

            void Write(BeamOperand operand)
            {
                Y(operand, false);
                if (operand.Kind == BeamOperandParameters.Y)
                    initialized |= BigInteger.One << (int)operand.Value;
            }

            void Fail(BeamOperand target)
            {
                if (target.Kind == BeamOperandParameters.Label && target.Value != function.InfoLabel)
                    Next(target.Value == function.EntryLabel ? function.FirstInstructionIndex : code.Labels[(int)target.Value], slots, initialized);
            }
            switch (instruction.Definition.Name)
            {
                case BeamLinkNames.Allocate:
                    if (slots != BeamFrameParameters.NoFrame)
                        throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                    slots = (int)operands[0].Value;
                    initialized = BigInteger.Zero;
                    break;
                case BeamLinkNames.Deallocate:
                    if (slots != operands[0].Value)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    slots = BeamFrameParameters.NoFrame;
                    initialized = BigInteger.Zero;
                    break;
                case BeamOnLoadNames.InitY:
                    foreach (BeamOperand operand in operands.Skip(BeamFrameParameters.TupleCountPrefix))
                    {
                        Y(operand, false);
                        initialized |= BigInteger.One << (int)operand.Value;
                    }
                    break;
                case BeamLinkNames.Move:
                    Y(operands[0], true);
                    Write(operands[1]);
                    break;
                case BeamLinkNames.Bif:
                    Write(operands[1]);
                    break;
                case BeamLinkNames.PutTuple:
                    foreach (BeamOperand operand in operands.Skip(BeamLinkParameters.TupleListPrefixOperands))
                        Y(operand, true);
                    Write(operands[0]);
                    break;
                case BeamLinkNames.PutList:
                    Y(operands[0], true);
                    Y(operands[1], true);
                    Write(operands[2]);
                    break;
                case BeamOnLoadNames.TupleElement:
                    Y(operands[0], true);
                    Write(operands[2]);
                    break;
                case BeamLinkNames.LoopReceive:
                    BeamReceivePlan plan = receives[current.Pc];
                    foreach (int node in plan.MatcherInstructions)
                    {
                        BeamInstruction matcher = code.Instructions[node];
                        foreach (BeamOperand operand in matcher.Operands)
                            Y(operand, true);
                        if (matcher.Definition.Name == BeamLinkNames.RemoveMessage)
                            Next(node + 1, slots, initialized);
                    }
                    Next(plan.TimeoutInstructionIndex + 1, slots, initialized);
                    continue;
                case BeamRegisterNames.Call:
                case BeamLambdaNames.Call:
                case BeamLinkNames.ExternalCall:
                    if (slots == BeamFrameParameters.NoFrame)
                        throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                    if (instruction.Definition.Name == BeamLambdaNames.Call)
                        Y(operands[2], true);
                    break;
                case BeamLinkNames.GuardBif:
                    Y(operands[3], true);
                    Y(operands[4], true);
                    Fail(operands[0]);
                    Write(operands[5]);
                    break;
                case BeamLinkNames.IntegerTest:
                case BeamOnLoadNames.PidTest:
                case BeamOnLoadNames.TaggedTupleTest:
                    Y(operands[1], true);
                    Fail(operands[0]);
                    break;
                case BeamLinkNames.EqualityTest:
                    Y(operands[1], true);
                    Y(operands[2], true);
                    Fail(operands[0]);
                    break;
                case BeamOnLoadNames.BadMatch:
                    Y(operands[0], true);
                    continue;
                case BeamOnLoadNames.ExternalLast:
                    if (slots != operands[2].Value)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    continue;
                case BeamLinkNames.Return:
                case BeamLinkNames.LocalCall:
                case BeamLinkNames.ExternalTailCall:
                    if (slots != BeamFrameParameters.NoFrame)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    continue;
            }
            Next(current.Pc + 1, slots, initialized);
        }
    }
}
