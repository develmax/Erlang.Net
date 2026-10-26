namespace Erlang;

internal static class BeamRecordConstruction
{
    internal static Term Create(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        registers.CheckLive((int)operands[BeamRecordConstructionNames.Live].Value);
        string module = code.Structure.Atoms[1]!;
        string name = code.Structure.Atoms[(int)operands[BeamRecordConstructionNames.Name].Value]!;
        var fields = new List<KeyValuePair<Atom, Term>>();
        for (int index = BeamRecordConstructionNames.FirstField; index < operands.Count; index += BeamRecordConstructionNames.FieldWidth)
            fields.Add(new(
                Term.A(code.Structure.Atoms[(int)operands[index].Value]!),
                BeamRegisterInterpreter.Read(code, registers, operands[index + 1])
            ));

        return NativeRecordLanguage.Construct(
            module,
            code.NativeRecords.SingleOrDefault(definition => definition.Name == name),
            name,
            fields,
            false
        );
    }
}
