namespace Erlang;

internal static class BeamCollectionExecution
{
    internal static Term UpdateRecord(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        Term source = BeamRegisterInterpreter.Read(code, registers, operands[2]);
        if (source is not TupleTerm record || record.Items.Count != operands[1].Value)
            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
        // Read all sources before the caller can overwrite an aliased destination.
        Term[] values = new Term[(operands.Count - 5) / 2];
        for (int field = 6, index = 0; field < operands.Count; field += 2, index++)
            values[index] = BeamRegisterInterpreter.Read(code, registers, operands[field]);
        Term[] items = record.Items.ToArray();
        for (int field = 5, index = 0; field < operands.Count; field += 2, index++)
            items[(int)operands[field].Value - 1] = values[index];

        return new TupleTerm(items);
    }

    internal static double ToFloat(Term source)
    {
        double value = source switch
        {
            Integer integer => (double)integer.Value,
            FloatTerm floating => floating.Value,
            _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
        };
        if (!double.IsFinite(value))
            throw new ErlangException(ErlangErrorReasons.BadArithmetic);

        return value;
    }
}
