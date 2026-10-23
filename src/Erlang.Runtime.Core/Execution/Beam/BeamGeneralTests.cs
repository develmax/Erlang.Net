namespace Erlang;

internal static class BeamGeneralTests
{
    internal static bool Match(BeamLinkedCode code, BeamRegisterState registers, BeamInstruction instruction)
    {
        var operands = instruction.Operands;
        Term value = BeamRegisterInterpreter.Read(code, registers, operands[1]);

        return instruction.Definition.Name switch
        {
            BeamGeneralOpcodeNames.NilTest => value is Nil,
            BeamGeneralOpcodeNames.NonemptyListTest => value is Cons,
            BeamGeneralOpcodeNames.ListTest => value is Cons or Nil,
            BeamGeneralOpcodeNames.TupleTest => value is TupleTerm,
            BeamGeneralOpcodeNames.FloatTest => value is FloatTerm,
            BeamGeneralOpcodeNames.MapTest => value is MapTerm or HamtBitmapTerm429,
            BeamGeneralOpcodeNames.NumberTest => value is Integer or FloatTerm,
            BeamGeneralOpcodeNames.BinaryTest => value is BitString bits && bits.BitLength % 8 == 0,
            BeamGeneralOpcodeNames.ArithmeticEqual => value.NumericEquals(BeamRegisterInterpreter.Read(code, registers, operands[2])),
            BeamGeneralOpcodeNames.ArithmeticNotEqual => !value.NumericEquals(BeamRegisterInterpreter.Read(code, registers, operands[2])),
            BeamGeneralOpcodeNames.ExactNotEqual => !value.Equals(BeamRegisterInterpreter.Read(code, registers, operands[2])),
            BeamGeneralOpcodeNames.AtomTest => value is Atom,
            BeamGeneralOpcodeNames.BooleanTest => value is Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False },
            BeamGeneralOpcodeNames.ReferenceTest => value is ReferenceTerm,
            BeamGeneralOpcodeNames.AnyFunctionTest => value is FunctionTerm,
            BeamGeneralOpcodeNames.BitStringTest => value is BitString,
            BeamGeneralOpcodeNames.PortTest => value is PortTerm,
            BeamGeneralOpcodeNames.NativeRecordTest => value is NativeRecordTerm,
            BeamGeneralOpcodeNames.TupleArity => value is TupleTerm tuple && tuple.Items.Count == operands[2].Value,
            BeamGeneralOpcodeNames.FunctionTest => value is FunctionTerm function
                && BeamRegisterInterpreter.Read(code, registers, operands[2]) is Integer arity && arity.Value == function.Arity,
            BeamGeneralOpcodeNames.GreaterOrEqual => TermOrder.Compare(value, BeamRegisterInterpreter.Read(code, registers, operands[2])) >= 0,
            BeamGeneralOpcodeNames.Less => TermOrder.Compare(value, BeamRegisterInterpreter.Read(code, registers, operands[2])) < 0,
            _ => throw new NotSupportedException(BeamRegisterNames.Unsupported)
        };
    }
}
