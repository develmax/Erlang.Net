namespace Erlang;

internal static class BeamReceivePlanner
{
    internal static bool WritesFrame(BeamLinkedCode code, BeamReceivePlan plan)
        => plan.Callback is not null || plan.MatcherInstructions.Any(index => code.Instructions[index].Definition.Name switch
        {
            BeamLinkNames.Move => code.Instructions[index].Operands[1].Kind == BeamOperandParameters.Y,
            BeamLinkNames.PutTuple => code.Instructions[index].Operands[0].Kind == BeamOperandParameters.Y,
            _ => false
        });

    internal static BeamReceivePlan BuildDelay(BeamLinkedCode code, BeamLinkedFunction function, int wait)
    {
        BeamInstruction instruction = code.Instructions[wait];
        // OTP emits a bare timeout for receive-after-zero without clauses.
        // It is the terminal point of its own immediate delay plan.
        if (instruction.Definition.Name == BeamLinkNames.Timeout)
            return new(
                wait,
                BeamReceiveNames.NoMessageRegister,
                wait,
                wait,
                new BeamOperand(BeamOperandParameters.Integer, 0),
                new HashSet<int>()
            );
        if (instruction.Definition.Name is not (BeamLinkNames.WaitTimeout or BeamLinkNames.Wait)
            || SkipMetadata(code, Label(code, instruction.Operands[0])) != wait)
            throw new NotSupportedException(BeamReceiveNames.InvalidPlan);
        BeamOperand? timeout = null;
        int expired = BeamReceiveNames.NoTimeoutInstruction;
        if (instruction.Definition.Name == BeamLinkNames.WaitTimeout)
        {
            timeout = instruction.Operands[1];
            Source(timeout);
            expired = SkipMetadata(code, wait + 1);
            if (expired >= function.EndInstructionIndex || code.Instructions[expired].Definition.Name != BeamLinkNames.Timeout)
                throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
        }

        return new(
            wait,
            BeamReceiveNames.NoMessageRegister,
            wait,
            expired,
            timeout,
            new HashSet<int>()
        );
    }

    public static BeamReceivePlan Build(BeamLinkedCode code, BeamLinkedFunction function, int scan)
    {
        var loop = code.Instructions[scan];
        if (loop.Operands[1].Kind != BeamOperandParameters.X)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
        int wait = SkipMetadata(code, Label(code, loop.Operands[0]));
        if (wait < function.FirstInstructionIndex || wait >= function.EndInstructionIndex
            || code.Instructions[wait].Definition.Name is not (BeamLinkNames.WaitTimeout or BeamLinkNames.Wait or BeamLinkNames.Timeout))
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
        var waiting = code.Instructions[wait];
        bool immediateTimeout = waiting.Definition.Name == BeamLinkNames.Timeout;
        if (!immediateTimeout && SkipMetadata(code, Label(code, waiting.Operands[0])) != scan)
            throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
        BeamOperand? timeout = immediateTimeout ? new(BeamOperandParameters.Integer, 0) : null;
        int expired = immediateTimeout ? wait : BeamReceiveNames.NoTimeoutInstruction;
        if (waiting.Definition.Name == BeamLinkNames.WaitTimeout)
        {
            timeout = waiting.Operands[1];
            Source(timeout);
            // Matcher scratch is cloned into X. Only stable Y or literal
            // timeout values can be read before the complete mailbox scan.
            if (timeout.Kind == BeamOperandParameters.X)
                throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
            expired = SkipMetadata(code, wait + 1);
            if (expired >= function.EndInstructionIndex || code.Instructions[expired].Definition.Name != BeamLinkNames.Timeout)
                throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
        }
        if (BeamReceiveCallbackPlan.Read(code, function, scan) is { } callback)
            return new(
                scan,
                (int)loop.Operands[1].Value,
                wait,
                expired,
                timeout,
                callback.Instructions
            )
            {
                Callback = callback.Callback
            };
        var seen = new HashSet<int>();
        var active = new HashSet<int>();
        bool success = false;

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
                    break;
                case BeamLinkNames.Label:
                case BeamLinkNames.Line:
                    Visit(pc + 1);
                    break;
                case BeamReceiveMarkerNames.Clear:
                    if (operands[0].Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                        throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                    RequireCommit(pc + 1);
                    Visit(pc + 1);
                    break;
                case BeamLinkNames.HeapTest:
                    Visit(pc + 1);
                    break;
                case BeamLinkNames.Move:
                    if (operands[1].Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                        throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                    Source(operands[0]);
                    if (operands[1].Kind == BeamOperandParameters.Y)
                        RequireCommit(pc + 1);
                    Visit(pc + 1);
                    break;
                case BeamGeneralOpcodeNames.FunctionTest:
                    Source(operands[2]);
                    goto case BeamGeneralOpcodeNames.AnyFunctionTest;
                case BeamGeneralOpcodeNames.NonemptyListTest:
                case BeamGeneralOpcodeNames.NilTest:
                case BeamGeneralOpcodeNames.ListTest:
                case BeamGeneralOpcodeNames.MapTest:
                case BeamGeneralOpcodeNames.TupleTest:
                case BeamGeneralOpcodeNames.TupleArity:
                case BeamGeneralOpcodeNames.AtomTest:
                case BeamGeneralOpcodeNames.ReferenceTest:
                case BeamGeneralOpcodeNames.BooleanTest:
                case BeamGeneralOpcodeNames.BinaryTest:
                case BeamGeneralOpcodeNames.AnyFunctionTest:
                case BeamOnLoadNames.PidTest:
                case BeamOnLoadNames.TaggedTupleTest:
                    Source(operands[1]);
                    Visit(Label(code, operands[0]));
                    Visit(pc + 1);
                    break;
                case BeamOnLoadNames.TupleElement:
                    Source(operands[0]);
                    if (operands[2].Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                        throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                    if (operands[2].Kind == BeamOperandParameters.Y)
                        RequireCommit(pc + 1);
                    Visit(pc + 1);
                    break;
                case BeamGeneralOpcodeNames.GetList:
                case BeamGeneralOpcodeNames.GetHead:
                case BeamGeneralOpcodeNames.GetTail:
                    Source(operands[0]);
                    foreach (BeamOperand destination in operands.Skip(1))
                    {
                        if (destination.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                        if (destination.Kind == BeamOperandParameters.Y)
                            RequireCommit(pc + 1);
                    }
                    Visit(pc + 1);
                    break;
                case BeamGeneralOpcodeNames.SelectTupleArity:
                case BeamGeneralOpcodeNames.SelectValue:
                    Source(operands[0]);
                    Visit(Label(code, operands[1]));
                    for (int target = 4; target < operands.Count; target += 2)
                        Visit(Label(code, operands[target]));
                    break;
                case BeamGeneralOpcodeNames.MapElements:
                    Source(operands[1]);
                    for (int pair = 3; pair < operands.Count; pair += 2)
                    {
                        Source(operands[pair]);
                        if (operands[pair + 1].Kind != BeamOperandParameters.X)
                            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                    }
                    Visit(Label(code, operands[0]));
                    Visit(pc + 1);
                    break;
                case BeamGeneralOpcodeNames.MapFields:
                    Source(operands[1]);
                    foreach (BeamOperand key in operands.Skip(3))
                        Source(key);
                    Visit(Label(code, operands[0]));
                    Visit(pc + 1);
                    break;
                case BeamLinkNames.IntegerTest:
                    Source(operands[1]);
                    Visit(Label(code, operands[0]));
                    Visit(pc + 1);
                    break;
                case BeamLinkNames.EqualityTest:
                case BeamGeneralOpcodeNames.GreaterOrEqual:
                case BeamGeneralOpcodeNames.Less:
                case BeamGeneralOpcodeNames.ExactNotEqual:
                case BeamGeneralOpcodeNames.ArithmeticNotEqual:
                case BeamGeneralOpcodeNames.ArithmeticEqual:
                    Source(operands[1]);
                    Source(operands[2]);
                    Visit(Label(code, operands[0]));
                    Visit(pc + 1);
                    break;
                case BeamBinaryNames.StartLegacy:
                    _ = BeamReceiveBinaryPairs.Match(code, pc);
                    RequireRejection(Label(code, operands[0]));
                    // The adjacent match has no label entry. Its private context
                    // exists only within the fused candidate operation.
                    seen.Add(pc + 1);
                    Visit(Label(code, operands[0]));
                    Visit(pc + 2);
                    break;
                case BeamLinkNames.Bif:
                    _ = BeamReceivePureBifs.Import(code, instruction);
                    if (operands[1].Kind != BeamOperandParameters.X)
                        throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                    Visit(pc + 1);
                    break;
                case BeamGeneralOpcodeNames.UnaryBif:
                case BeamGeneralOpcodeNames.UnaryGcBif:
                    _ = BeamReceivePureBifs.Import(code, instruction);
                    bool gc = instruction.Definition.Name == BeamGeneralOpcodeNames.UnaryGcBif;
                    if (operands[gc ? 4 : 3].Kind != BeamOperandParameters.X)
                        throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                    Source(operands[gc ? 3 : 2]);
                    if (operands[0].Kind != BeamOperandParameters.Failure)
                        Visit(Label(code, operands[0]));
                    Visit(pc + 1);
                    break;
                case BeamGeneralOpcodeNames.BinaryBif:
                case BeamLinkNames.GuardBif:
                    _ = BeamReceivePureBifs.Import(code, instruction);
                    int binaryOffset = instruction.Definition.Name == BeamLinkNames.GuardBif ? 1 : 0;
                    if (operands[4 + binaryOffset].Kind != BeamOperandParameters.X)
                        throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                    Source(operands[2 + binaryOffset]);
                    Source(operands[3 + binaryOffset]);
                    if (operands[0].Kind != BeamOperandParameters.Failure)
                        Visit(Label(code, operands[0]));
                    Visit(pc + 1);
                    break;
                case BeamLinkNames.PutTuple:
                    if (operands[0].Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                        throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
                    foreach (BeamOperand value in operands.Skip(BeamLinkParameters.TupleListPrefixOperands))
                        Source(value);
                    if (operands[0].Kind == BeamOperandParameters.Y)
                        RequireCommit(pc + 1);
                    Visit(pc + 1);
                    break;
                case BeamGeneralOpcodeNames.Jump:
                    Visit(Label(code, operands[0]));
                    break;
                default:
                    throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher)
                    {
                        Data = { [nameof(BeamInstruction)] = (instruction.Definition.Name, pc) }
                    };
            }
            active.Remove(pc);
        }

        void RequireRejection(int pc)
        {
            while (pc < function.EndInstructionIndex && code.Instructions[pc].Definition.Name is BeamLinkNames.Label or BeamLinkNames.Line)
                pc++;
            if (pc >= function.EndInstructionIndex || code.Instructions[pc].Definition.Name != BeamLinkNames.LoopReceiveEnd
                || SkipMetadata(code, Label(code, code.Instructions[pc].Operands[0])) != scan)
                throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
        }

        void RequireCommit(int pc)
        {
            while (pc < function.EndInstructionIndex)
            {
                string name = code.Instructions[pc++].Definition.Name;
                if (name == BeamLinkNames.RemoveMessage)
                    return;
                if (name is not (BeamLinkNames.Label or BeamLinkNames.Line or BeamLinkNames.Move
                    or BeamOnLoadNames.TupleElement or BeamLinkNames.PutTuple or BeamReceiveMarkerNames.Clear))
                    throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
            }
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
        }
        Visit(scan + 1);
        if (!success)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);

        return new(
            scan,
            (int)loop.Operands[1].Value,
            wait,
            expired,
            timeout,
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
        if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y or BeamOperandParameters.Integer or BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue))
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
    }
}
