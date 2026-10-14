namespace Erlang;

internal static class BeamRegisterValidation
{
    public static void Validate(BeamLinkedCode code, BeamLinkedFunction entry)
    {
        if (code.OnLoadEntryLabel is not null)
        {
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
        }
        if (code.Lambdas.Count != 0)
        {
            _ = BeamChecksumReader.Read(code);
        }
        var visited = new HashSet<int>();
        var pending = new Stack<BeamLinkedFunction>();
        pending.Push(entry);
        while (pending.TryPop(out BeamLinkedFunction? function))
        {
            if (!visited.Add(function.EntryLabel))
            {
                continue;
            }
            var receives = new Dictionary<int, BeamReceivePlan>();
            var handled = new HashSet<int>();
            for (int scan = function.FirstInstructionIndex; scan < function.EndInstructionIndex; scan++)
            {
                if (code.Instructions[scan].Definition.Name != BeamLinkNames.LoopReceive)
                    continue;
                BeamReceivePlan plan = BeamReceivePlanner.Build(code, function, scan);
                receives.Add(scan, plan);
                handled.UnionWith(plan.MatcherInstructions);
                handled.Add(plan.WaitInstructionIndex);
                handled.Add(plan.TimeoutInstructionIndex);
            }
            for (int index = function.FirstInstructionIndex; index < function.EndInstructionIndex; index++)
            {
                BeamInstruction instruction = code.Instructions[index];
                var operands = instruction.Operands;
                switch (instruction.Definition.Name)
                {
                    case BeamLinkNames.Label:
                    case BeamLinkNames.Line:
                    case BeamLinkNames.Return:
                        break;
                    case BeamLinkNames.Allocate:
                        if (operands[0].Value != 0)
                            throw new NotSupportedException(BeamReceiveNames.InvalidFrame);
                        Live(operands[1]);
                        break;
                    case BeamLinkNames.Deallocate:
                        if (operands[0].Value != 0)
                            throw new NotSupportedException(BeamReceiveNames.InvalidFrame);
                        break;
                    case BeamLinkNames.Bif:
                        SelfImport(code, (int)operands[0].Value);
                        Destination(operands[1]);
                        break;
                    case BeamLinkNames.Send:
                    case BeamLinkNames.LoopReceive:
                        break;
                    case BeamLinkNames.RemoveMessage:
                    case BeamLinkNames.LoopReceiveEnd:
                    case BeamLinkNames.WaitTimeout:
                    case BeamLinkNames.Timeout:
                        if (!handled.Contains(index))
                            throw new NotSupportedException(BeamReceiveNames.InvalidPlan);
                        break;
                    case BeamLinkNames.Move:
                        Source(operands[0]);
                        Destination(operands[1]);
                        break;
                    case BeamLambdaNames.Create:
                        Destination(operands[1]);
                        foreach (BeamOperand operand in operands.Skip(BeamLambdaNames.EnvironmentPrefixOperands))
                        {
                            Source(operand);
                        }
                        pending.Push(code.Functions.Single(target => target.EntryLabel == code.Lambdas[(int)operands[0].Value].Label));
                        break;
                    case BeamLambdaNames.Call:
                        if (operands[0].Kind != BeamOperandParameters.Unsigned)
                        {
                            throw new NotSupportedException(BeamRegisterNames.Unsupported);
                        }
                        Source(operands[2]);
                        pending.Push(code.Functions.Single(target => target.EntryLabel == code.Lambdas[(int)operands[0].Value].Label));
                        break;
                    case BeamRegisterNames.Call:
                    case BeamLinkNames.LocalCall:
                        pending.Push(code.Functions.Single(target => target.EntryLabel == operands[1].Value));
                        break;
                    case BeamLinkNames.IntegerTest:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        break;
                    case BeamLinkNames.EqualityTest:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        Source(operands[2]);
                        break;
                    case BeamLinkNames.ExternalTailCall:
                        _ = ArithmeticImport(code, (int)operands[1].Value);
                        break;
                    case BeamLinkNames.GuardBif:
                        _ = ArithmeticImport(code, (int)operands[2].Value);
                        Branch(code, function, operands[0]);
                        Live(operands[1]);
                        Source(operands[3]);
                        Source(operands[4]);
                        Destination(operands[5]);
                        break;
                    case BeamLinkNames.HeapTest:
                        Live(operands[1]);
                        break;
                    case BeamLinkNames.PutList:
                        Source(operands[0]);
                        Source(operands[1]);
                        Destination(operands[2]);
                        break;
                    case BeamLinkNames.PutTuple:
                        Destination(operands[0]);
                        foreach (BeamOperand operand in operands.Skip(BeamLinkParameters.TupleListPrefixOperands))
                        {
                            Source(operand);
                        }
                        break;
                    default:
                        throw new NotSupportedException(BeamRegisterNames.Unsupported);
                }
            }
            BeamZeroFrameValidation.Validate(code, function, receives);
        }
    }

    public static void SelfImport(BeamLinkedCode code, int index)
    {
        BeamImport import = code.Structure.Imports[index];
        if (code.Structure.Atoms[import.ModuleAtomIndex] != ErlangModuleNames.Module
            || code.Structure.Atoms[import.FunctionAtomIndex] != ErlangModuleNames.Self || import.Arity != 0)
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
    }

    public static string ArithmeticImport(BeamLinkedCode code, int index)
    {
        BeamImport import = code.Structure.Imports[index];
        string module = code.Structure.Atoms[import.ModuleAtomIndex]!;
        string function = code.Structure.Atoms[import.FunctionAtomIndex]!;
        if (module != BeamRegisterNames.Erlang || import.Arity != 2 || function is not (BeamRegisterNames.Add or BeamRegisterNames.Subtract))
        {
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
        }

        return function;
    }

    private static void Branch(BeamLinkedCode code, BeamLinkedFunction function, BeamOperand operand)
    {
        if (operand.Kind == BeamOperandParameters.Failure || operand.Value == function.InfoLabel || operand.Value == function.EntryLabel)
        {
            return;
        }
        int target = code.Labels[(int)operand.Value];
        if (target < function.FirstInstructionIndex || target >= function.EndInstructionIndex)
        {
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
        }
    }

    private static void Live(BeamOperand operand)
    {
        if (operand.Value > BeamOperandParameters.MaximumRegister)
        {
            throw new InvalidDataException(BeamRegisterNames.Unsupported);
        }
    }

    private static void Source(BeamOperand operand)
    {
        if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Integer or BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue))
        {
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
        }
    }

    private static void Destination(BeamOperand operand)
    {
        if (operand.Kind != BeamOperandParameters.X)
        {
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
        }
    }
}
