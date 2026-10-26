namespace Erlang;

internal static class BeamRegisterValidation
{
    public static void Validate(BeamLinkedCode code, BeamLinkedFunction entry)
    {
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
            BeamTupleSelectValidation.Validate(code, function);
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
                if (plan.HasTimeout)
                    handled.Add(plan.TimeoutInstructionIndex);
            }
            for (int wait = function.FirstInstructionIndex; wait < function.EndInstructionIndex; wait++)
            {
                if (handled.Contains(wait) || code.Instructions[wait].Definition.Name is not (BeamLinkNames.WaitTimeout or BeamLinkNames.Wait or BeamLinkNames.Timeout))
                    continue;
                BeamReceivePlan delay = BeamReceivePlanner.BuildDelay(code, function, wait);
                receives.Add(wait, delay);
                handled.Add(wait);
                if (delay.HasTimeout)
                    handled.Add(delay.TimeoutInstructionIndex);
            }
            for (int index = function.FirstInstructionIndex; index < function.EndInstructionIndex; index++)
            {
                BeamInstruction instruction = code.Instructions[index];
                var operands = instruction.Operands;
                switch (instruction.Definition.Name)
                {
                    case BeamRecordConstructionNames.Opcode:
                        Live(operands[BeamRecordConstructionNames.Live]);
                        for (int field = BeamRecordConstructionNames.FirstField; field < operands.Count; field += BeamRecordConstructionNames.FieldWidth)
                            Source(operands[field + 1]);
                        Destination(operands[BeamRecordConstructionNames.Destination]);
                        break;
                    case BeamCollectionOpcodeNames.UpdateRecord:
                        Source(operands[2]);
                        for (int field = 6; field < operands.Count; field += 2)
                            Source(operands[field]);
                        Destination(operands[3]);
                        break;
                    case BeamCollectionOpcodeNames.BadRecord:
                        Source(operands[0]);
                        break;
                    case BeamCollectionOpcodeNames.FloatMove:
                        if (operands[0].Kind == BeamOperandParameters.Float)
                            Destination(operands[1]);
                        else
                            Source(operands[0]);
                        break;
                    case BeamCollectionOpcodeNames.FloatConvert:
                        Source(operands[0]);
                        break;
                    case BeamCollectionOpcodeNames.FloatNegate:
                    case BeamCollectionOpcodeNames.FloatMultiply:
                    case BeamCollectionOpcodeNames.FloatAdd:
                    case BeamCollectionOpcodeNames.FloatSubtract:
                    case BeamCollectionOpcodeNames.FloatDivide:
                        break;
                    case BeamBinaryNames.Create:
                        Branch(code, function, operands[0]);
                        Live(operands[2]);
                        foreach (BeamOperand value in operands.Skip(BeamBinaryNames.CreatePrefix))
                            if (value.Kind is BeamOperandParameters.X or BeamOperandParameters.Y)
                                Source(value);
                        Destination(operands[4]);
                        break;
                    case BeamBinaryNames.Start:
                        Live(operands[1]);
                        Source(operands[2]);
                        Destination(operands[3]);
                        break;
                    case BeamBinaryNames.StartLegacy:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        Live(operands[2]);
                        Destination(operands[3]);
                        break;
                    case BeamBinaryNames.ExtractInteger:
                    case BeamFloatStringNames.ExtractFloat:
                    case BeamBinaryNames.ExtractBinary:
                        Branch(code, function, operands[0]);
                        Destination(operands[1]);
                        Live(operands[2]);
                        Source(operands[3]);
                        Destination(operands[6]);
                        break;
                    case BeamBinaryNames.ExtractUtf8:
                    case BeamBinaryNames.ExtractUtf16:
                    case BeamBinaryNames.ExtractUtf32:
                    case BeamBinaryNames.SkipUtf16:
                    case BeamBinaryNames.SkipUtf32:
                    case BeamBinaryNames.SkipUtf8:
                        Branch(code, function, operands[0]);
                        Destination(operands[1]);
                        Live(operands[2]);
                        if (instruction.Definition.Name is not (BeamBinaryNames.SkipUtf8 or BeamBinaryNames.SkipUtf16 or BeamBinaryNames.SkipUtf32))
                            Destination(operands[4]);
                        break;
                    case BeamFloatStringNames.MatchString:
                        Branch(code, function, operands[0]);
                        Destination(operands[1]);
                        break;
                    case BeamBinaryNames.SkipBits:
                        Branch(code, function, operands[0]);
                        Destination(operands[1]);
                        Source(operands[2]);
                        break;
                    case BeamBinaryNames.ExtractTail:
                    case BeamBinaryNames.SavePosition:
                        Destination(operands[0]);
                        Destination(operands[1]);
                        Live(operands[2]);
                        break;
                    case BeamBinaryNames.RestorePosition:
                        Destination(operands[0]);
                        Destination(operands[1]);
                        break;
                    case BeamBinaryNames.InitializeWritable:
                        break;
                    case BeamBinaryNames.Match:
                        Branch(code, function, operands[0]);
                        Destination(operands[1]);
                        foreach (BeamBinaryCommand command in BeamBinaryCommands.Read(code.Structure, operands))
                        {
                            if (command.Live >= 0)
                                Live(new(BeamOperandParameters.Unsigned, command.Live));
                            if (command.Destination is { } destination)
                                Destination(destination);
                        }
                        break;
                    case BeamReceiveMarkerNames.Reserve:
                        Destination(operands[0]);
                        break;
                    case BeamReceiveMarkerNames.Bind:
                        Destination(operands[0]);
                        Destination(operands[1]);
                        break;
                    case BeamReceiveMarkerNames.Clear:
                    case BeamReceiveMarkerNames.Use:
                        Destination(operands[0]);
                        break;
                    case BeamCatchNames.Install:
                    case BeamTryNames.Install:
                        if (operands[0].Kind != BeamOperandParameters.Y
                            || operands[1].Kind != BeamOperandParameters.Label
                            || operands[1].Value == function.InfoLabel
                            || operands[1].Value == function.EntryLabel)
                            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                        Branch(code, function, operands[1]);
                        break;
                    case BeamTryNames.Case:
                    case BeamCatchNames.End:
                    case BeamTryNames.End:
                        if (operands[0].Kind != BeamOperandParameters.Y)
                            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                        break;
                    case BeamTryNames.BuildStacktrace:
                    case BeamTryNames.RawRaise:
                        break;
                    case BeamTryNames.CaseEnd:
                        Source(operands[0]);
                        break;
                    case BeamTryNames.Raise:
                        Source(operands[0]);
                        Source(operands[1]);
                        break;
                    case BeamGeneralOpcodeNames.NilTest:
                    case BeamGeneralOpcodeNames.NonemptyListTest:
                    case BeamGeneralOpcodeNames.ListTest:
                    case BeamGeneralOpcodeNames.TupleTest:
                    case BeamGeneralOpcodeNames.FloatTest:
                    case BeamGeneralOpcodeNames.MapTest:
                    case BeamGeneralOpcodeNames.NumberTest:
                    case BeamGeneralOpcodeNames.BinaryTest:
                    case BeamGeneralOpcodeNames.AtomTest:
                    case BeamGeneralOpcodeNames.BooleanTest:
                    case BeamGeneralOpcodeNames.ReferenceTest:
                    case BeamGeneralOpcodeNames.AnyFunctionTest:
                    case BeamGeneralOpcodeNames.BitStringTest:
                    case BeamGeneralOpcodeNames.PortTest:
                    case BeamGeneralOpcodeNames.NativeRecordTest:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        break;
                    case BeamGeneralOpcodeNames.TupleArity:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        break;
                    case BeamGeneralOpcodeNames.FunctionTest:
                    case BeamGeneralOpcodeNames.ArithmeticEqual:
                    case BeamGeneralOpcodeNames.ArithmeticNotEqual:
                    case BeamGeneralOpcodeNames.ExactNotEqual:
                    case BeamGeneralOpcodeNames.GreaterOrEqual:
                    case BeamGeneralOpcodeNames.Less:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        Source(operands[2]);
                        break;
                    case BeamGeneralOpcodeNames.GetList:
                        Source(operands[0]);
                        Destination(operands[1]);
                        Destination(operands[2]);
                        break;
                    case BeamGeneralOpcodeNames.GetHead:
                    case BeamGeneralOpcodeNames.GetTail:
                        Source(operands[0]);
                        Destination(operands[1]);
                        break;
                    case BeamGeneralOpcodeNames.Swap:
                        Destination(operands[0]);
                        Destination(operands[1]);
                        break;
                    case BeamGeneralOpcodeNames.Trim:
                        if (operands[0].Value > BeamFrameParameters.MaximumSlots || operands[1].Value > BeamFrameParameters.MaximumSlots)
                            throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                        break;
                    case BeamGeneralOpcodeNames.Jump:
                        Branch(code, function, operands[0]);
                        break;
                    case BeamGeneralOpcodeNames.SelectValue:
                    case BeamGeneralOpcodeNames.SelectTupleArity:
                        Source(operands[0]);
                        Branch(code, function, operands[1]);
                        for (int target = 4; target < operands.Count; target += 2)
                            Branch(code, function, operands[target]);
                        break;
                    case BeamGeneralOpcodeNames.BinaryBif:
                        _ = BinaryImport(code, (int)operands[1].Value);
                        Branch(code, function, operands[0]);
                        Source(operands[2]);
                        Source(operands[3]);
                        Destination(operands[4]);
                        break;
                    case BeamApplyNames.Opcode:
                    case BeamApplyNames.Last:
                        if (operands[0].Value + BeamApplyNames.ExtraRegisters > BeamOperandParameters.MaximumRegister)
                            throw new InvalidDataException(BeamRegisterNames.Unsupported);
                        break;
                    case BeamGeneralOpcodeNames.DynamicCall:
                        Live(operands[0]);
                        break;
                    case BeamGeneralOpcodeNames.MapFields:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        foreach (BeamOperand key in operands.Skip(3))
                            Source(key);
                        break;
                    case BeamGeneralOpcodeNames.MapElements:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        for (int pair = 3; pair < operands.Count; pair += 2)
                        {
                            Source(operands[pair]);
                            Destination(operands[pair + 1]);
                        }
                        break;
                    case BeamGeneralOpcodeNames.MapExact:
                    case BeamGeneralOpcodeNames.MapAssociate:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        Destination(operands[2]);
                        Live(operands[3]);
                        foreach (BeamOperand value in operands.Skip(5))
                            Source(value);
                        break;
                    case BeamGeneralOpcodeNames.UnaryBif:
                        _ = UnaryImport(code, (int)operands[1].Value);
                        Branch(code, function, operands[0]);
                        Source(operands[2]);
                        Destination(operands[3]);
                        break;
                    case BeamGeneralOpcodeNames.UnaryGcBif:
                        _ = UnaryImport(code, (int)operands[2].Value);
                        Branch(code, function, operands[0]);
                        Live(operands[1]);
                        Source(operands[3]);
                        Destination(operands[4]);
                        break;
                    case BeamGeneralOpcodeNames.TernaryBif:
                        _ = TernaryBifImport(code, (int)operands[1].Value);
                        Branch(code, function, operands[0]);
                        foreach (BeamOperand source in operands.Skip(2).Take(3))
                            Source(source);
                        Destination(operands[5]);
                        break;
                    case BeamGeneralOpcodeNames.TernaryGcBif:
                        _ = TernaryImport(code, (int)operands[2].Value);
                        Branch(code, function, operands[0]);
                        Live(operands[1]);
                        foreach (BeamOperand source in operands.Skip(3).Take(3))
                            Source(source);
                        Destination(operands[6]);
                        break;
                    case BeamGeneralOpcodeNames.IfEnd:
                        break;
                    case BeamGeneralOpcodeNames.LocalLast:
                        Live(operands[0]);
                        pending.Push(code.Functions.Single(target => target.EntryLabel == operands[1].Value));
                        break;
                    case BeamGeneralOpcodeNames.CaseEnd:
                        Source(operands[0]);
                        break;
                    case BeamLinkNames.OnLoad:
                        if (function.EntryLabel != code.OnLoadEntryLabel)
                            throw new InvalidDataException(BeamOnLoadDiagnostics.OnLoadRequired);
                        break;
                    case BeamOnLoadNames.InitY:
                        break;
                    case BeamOnLoadNames.PidTest:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        break;
                    case BeamOnLoadNames.TaggedTupleTest:
                        Branch(code, function, operands[0]);
                        Source(operands[1]);
                        Source(operands[3]);
                        break;
                    case BeamOnLoadNames.TupleElement:
                        Source(operands[0]);
                        Destination(operands[2]);
                        break;
                    case BeamOnLoadNames.BadMatch:
                        Source(operands[0]);
                        break;
                    case BeamLinkNames.ExternalCall:
                    case BeamOnLoadNames.ExternalLast:
                        _ = BeamExternalImports.Resolve(code, (int)operands[1].Value);
                        break;
                    case BeamNifNames.Start:
                    case BeamLinkNames.Label:
                    case BeamLinkNames.Line:
                    case BeamLinkNames.Return:
                        break;
                    case BeamLinkNames.AllocateHeap:
                        if (operands[0].Value > BeamFrameParameters.MaximumSlots)
                            throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                        Live(operands[2]);
                        break;
                    case BeamLinkNames.Allocate:
                        if (operands[0].Value > BeamFrameParameters.MaximumSlots)
                            throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                        Live(operands[1]);
                        break;
                    case BeamLinkNames.Deallocate:
                        if (operands[0].Value > BeamFrameParameters.MaximumSlots)
                            throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
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
                    case BeamLinkNames.Wait:
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
                        Source(operands[2]);
                        if (operands[0].Kind == BeamOperandParameters.Unsigned)
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
                        _ = BeamExternalImports.Resolve(code, (int)operands[1].Value);
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
            BeamFrameValidation.Validate(code, function, receives);
        }
    }

    public static void SelfImport(BeamLinkedCode code, int index)
    {
        BeamImport import = code.Structure.Imports[index];
        if (code.Structure.Atoms[import.ModuleAtomIndex] != ErlangModuleNames.Module
            || code.Structure.Atoms[import.FunctionAtomIndex] is not (ErlangModuleNames.Self or ErlangProcessQueryNames.Node) || import.Arity != 0)
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
    }

    internal static BeamExternalImport TernaryBifImport(BeamLinkedCode code, int index)
    {
        BeamExternalImport import = BeamExternalImports.Resolve(code, index);
        if (import.Module != ErlangModuleNames.Module
            || import.Function is not (ErlangModuleNames.IsInteger or RecordPredicateNames.IsRecord) || import.Arity != 3)
            throw new NotSupportedException(BeamRegisterNames.Unsupported);

        return import;
    }

    internal static BeamExternalImport TernaryImport(BeamLinkedCode code, int index)
    {
        BeamExternalImport import = BeamExternalImports.Resolve(code, index);
        if (import.Module != ErlangModuleNames.Module || import.Function != ErlangDataBifNames.BinaryPart || import.Arity != 3)
            throw new NotSupportedException(BeamRegisterNames.Unsupported);

        return import;
    }

    public static string ArithmeticImport(BeamLinkedCode code, int index)
    {
        BeamExternalImport import = BeamExternalImports.Resolve(code, index);
        if (import.Module != ErlangModuleNames.Module || import.Arity != 2
            || import.Function is not (ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply
                or ErlangOperators.Divide or ErlangOperators.IntegerDivide or ErlangOperators.Remainder
                or ErlangOperators.BitwiseAnd or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor
                or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight))
        {
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
        }

        return import.Function;
    }

    internal static BeamExternalImport BinaryImport(BeamLinkedCode code, int index)
    {
        BeamExternalImport import = BeamExternalImports.Resolve(code, index);
        if (import.Module != ErlangModuleNames.Module || import.Arity != 2 || !NativeBuiltinCatalog.Contains(import.Module, import.Function, import.Arity))
            throw new NotSupportedException(BeamRegisterNames.Unsupported);

        return import;
    }

    internal static BeamExternalImport UnaryImport(BeamLinkedCode code, int index)
    {
        BeamExternalImport import = BeamExternalImports.Resolve(code, index);
        if (import.Module != ErlangModuleNames.Module || import.Arity != 1
            || !NativeBuiltinCatalog.Contains(import.Module, import.Function, import.Arity))
            throw new NotSupportedException(BeamRegisterNames.Unsupported);

        return import;
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
        if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y or BeamOperandParameters.Integer or BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue))
        {
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
        }
    }

    private static void Destination(BeamOperand operand)
    {
        if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
        {
            throw new NotSupportedException(BeamRegisterNames.Unsupported);
        }
    }
}
