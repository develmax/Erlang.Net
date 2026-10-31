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

        if (operands[BeamRecordConstructionNames.Source].Kind != BeamOperandParameters.Nil)
        {
            Term source = BeamRegisterInterpreter.Read(code, registers, operands[BeamRecordConstructionNames.Source]);

            // Pinned put_record ignores Id for a source-register update. The
            // preceding compiler tests own identity/access checks; preserve the
            // actual source definition and field set rather than reconstructing.
            return NativeRecordLanguage.Update(
                source,
                module,
                null,
                source is NativeRecordTerm record ? record.Module.Name : module,
                false,
                fields
            );
        }

        return NativeRecordLanguage.Construct(
            module,
            code.NativeRecords.SingleOrDefault(definition => definition.Name == name),
            name,
            fields,
            false
        );
    }
}
