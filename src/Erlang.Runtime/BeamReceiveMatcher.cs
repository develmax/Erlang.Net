namespace Erlang;

internal static class BeamReceiveMatcher
{
    public static BeamReceiveSelection? Select(
        BeamLinkedCode code,
        BeamReceivePlan plan,
        BeamRegisterState before,
        Term message,
        Pid self
    )
    {
        BeamRegisterState registers = before.Clone();
        registers.Write(plan.MessageRegister, message);
        var clearMarkers = new List<Term>();
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
                    if (operands[0].Kind is BeamOperandParameters.X or BeamOperandParameters.Y)
                        registers.CopyRegister(operands[0], operands[1]);
                    else
                        registers.Write(operands[1], Read(code, registers, operands[0]));
                    break;
                case BeamGeneralOpcodeNames.NonemptyListTest:
                    if (Read(code, registers, operands[1]) is not Cons)
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamGeneralOpcodeNames.TupleTest:
                    if (Read(code, registers, operands[1]) is not TupleTerm)
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamGeneralOpcodeNames.NilTest:
                case BeamGeneralOpcodeNames.ListTest:
                case BeamGeneralOpcodeNames.MapTest:
                case BeamGeneralOpcodeNames.TupleArity:
                case BeamGeneralOpcodeNames.AtomTest:
                case BeamGeneralOpcodeNames.ReferenceTest:
                case BeamGeneralOpcodeNames.BooleanTest:
                case BeamGeneralOpcodeNames.AnyFunctionTest:
                case BeamGeneralOpcodeNames.GreaterOrEqual:
                case BeamGeneralOpcodeNames.Less:
                case BeamGeneralOpcodeNames.ExactNotEqual:
                case BeamGeneralOpcodeNames.ArithmeticNotEqual:
                case BeamGeneralOpcodeNames.ArithmeticEqual:
                    if (!BeamGeneralTests.Match(code, registers, instruction))
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamBinaryNames.StartLegacy:
                    if (!BeamReceiveBinaryPairs.Execute(code, registers, pc - 1))
                        pc = code.Labels[(int)operands[0].Value];
                    else
                        pc++;
                    break;
                case BeamLinkNames.Bif:
                    _ = BeamReceivePureBifs.Import(code, instruction);
                    registers.Write(operands[1], self);
                    break;
                case BeamGeneralOpcodeNames.UnaryBif:
                case BeamGeneralOpcodeNames.UnaryGcBif:
                    // Selection never invokes registry bodies, callbacks or guest code.
                    BeamExternalImport unary = BeamReceivePureBifs.Import(code, instruction);
                    bool gc = instruction.Definition.Name == BeamGeneralOpcodeNames.UnaryGcBif;
                    Term argument = Read(code, registers, operands[gc ? 3 : 2]);
                    if (gc)
                        registers.CheckLive((int)operands[1].Value);
                    try
                    {
                        registers.Write(operands[gc ? 4 : 3], BeamReceivePureBifs.Unary(unary.Function, argument));
                    }
                    catch (ErlangException)
                    {
                        pc = code.Labels[(int)operands[0].Value];
                    }
                    break;
                case BeamGeneralOpcodeNames.BinaryBif:
                    _ = BeamReceivePureBifs.Import(code, instruction);
                    Term first = Read(code, registers, operands[2]);
                    Term second = Read(code, registers, operands[3]);
                    try
                    {
                        registers.Write(operands[4], BeamReceivePureBifs.Element(first, second));
                    }
                    catch (ErlangException)
                    {
                        pc = code.Labels[(int)operands[0].Value];
                    }
                    break;
                case BeamLinkNames.PutTuple:
                    Term[] values = operands.Skip(BeamLinkParameters.TupleListPrefixOperands).Select(value => Read(code, registers, value)).ToArray();
                    registers.Write(operands[0], Term.Tuple(values));
                    break;
                case BeamGeneralOpcodeNames.Jump:
                    pc = code.Labels[(int)operands[0].Value];
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
                case BeamGeneralOpcodeNames.GetList:
                    Cons list = (Cons)Read(code, registers, operands[0]);
                    registers.Write(operands[1], list.Head);
                    registers.Write(operands[2], list.Tail);
                    break;
                case BeamGeneralOpcodeNames.GetHead:
                    registers.Write(operands[1], ((Cons)Read(code, registers, operands[0])).Head);
                    break;
                case BeamGeneralOpcodeNames.GetTail:
                    registers.Write(operands[1], ((Cons)Read(code, registers, operands[0])).Tail);
                    break;
                case BeamGeneralOpcodeNames.SelectTupleArity:
                    pc = code.Labels[(int)BeamTupleSelectValidation.Select(Read(code, registers, operands[0]), operands).Value];
                    break;
                case BeamGeneralOpcodeNames.SelectValue:
                    Term selected = Read(code, registers, operands[0]);
                    BeamOperand target = operands[1];
                    for (int pair = 3; pair < operands.Count; pair += 2)
                        if (selected.Equals(Read(code, registers, operands[pair])))
                        {
                            target = operands[pair + 1];
                            break;
                        }
                    pc = code.Labels[(int)target.Value];
                    break;
                case BeamGeneralOpcodeNames.MapElements:
                    if (Read(code, registers, operands[1]) is not MapTerm elementMap)
                        throw new InvalidDataException(BeamLinkDiagnostics.OperandRole);
                    var extracted = new List<(BeamOperand Destination, Term Value)>();
                    bool missingElement = false;
                    for (int pair = 3; pair < operands.Count; pair += 2)
                    {
                        if (!elementMap.TryGet(Read(code, registers, operands[pair]), out Term? element))
                        {
                            missingElement = true;
                            break;
                        }
                        extracted.Add((operands[pair + 1], element!));
                    }
                    if (missingElement)
                        pc = code.Labels[(int)operands[0].Value];
                    else
                        foreach (var element in extracted)
                            registers.Write(element.Destination, element.Value);
                    break;
                case BeamGeneralOpcodeNames.MapFields:
                    if (Read(code, registers, operands[1]) is not MapTerm map
                        || operands.Skip(3).Any(key => !map.TryGet(Read(code, registers, key), out _)))
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamLinkNames.IntegerTest:
                    if (Read(code, registers, operands[1]) is not Integer)
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamLinkNames.EqualityTest:
                    if (!Read(code, registers, operands[1]).Equals(Read(code, registers, operands[2])))
                        pc = code.Labels[(int)operands[0].Value];
                    break;
                case BeamLinkNames.HeapTest:
                    registers.CheckLive((int)operands[1].Value);
                    break;
                case BeamReceiveMarkerNames.Clear:
                    clearMarkers.Add(Read(code, registers, operands[0]));
                    break;
                case BeamLinkNames.RemoveMessage:
                    return new(pc, registers, clearMarkers.ToArray());
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
