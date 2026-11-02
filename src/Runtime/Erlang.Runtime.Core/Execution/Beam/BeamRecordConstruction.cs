namespace Erlang;

internal static class BeamRecordConstruction
{
    internal static async ValueTask<Term> Create(
        ProcessContext context,
        BeamExecutionIdentity execution,
        BeamLinkedCode code,
        BeamRegisterState registers,
        IReadOnlyList<BeamOperand> operands
    )
    {
        registers.CheckLive((int)operands[BeamRecordConstructionNames.Live].Value);
        string module = code.Structure.Atoms[1]!;
        Term identity = BeamRegisterInterpreter.Read(code, registers, operands[BeamRecordConstructionNames.Name]);
        string name = identity is Atom local ? local.Name : ((Atom)((TupleTerm)identity).Items[1]).Name;
        var fields = new List<KeyValuePair<Atom, Term>>();
        for (int index = BeamRecordConstructionNames.FirstField; index < operands.Count; index += BeamRecordConstructionNames.FieldWidth)
            fields.Add(new(
                Term.A(code.Structure.Atoms[(int)operands[index].Value]!),
                BeamRegisterInterpreter.Read(code, registers, operands[index + 1])
            ));

        if (identity is TupleTerm imported)
            return await context.Runtime.Modules.ConstructNativeRecord313(
                context,
                ((Atom)imported.Items[0]).Name,
                name,
                fields
            );

        return NativeRecordLanguage.Construct313(
            module,
            code.NativeRecords.SingleOrDefault(definition => definition.Name == name),
            name,
            fields,
            false,
            template => context.ReadExecutingNativeRecordDefault313(template, execution.Owner)
        );
    }

    internal static Term ReadField(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        Term source = BeamRegisterInterpreter.Read(code, registers, operands[BeamRecordConstructionNames.FieldSource]);
        Term identity = BeamRegisterInterpreter.Read(code, registers, operands[BeamRecordConstructionNames.FieldIdentity]);
        var field = (Atom)BeamRegisterInterpreter.Read(code, registers, operands[BeamRecordConstructionNames.FieldName]);
        string caller = code.Structure.Atoms[1]!;
        if (identity is Atom local)
            return NativeRecordLanguage.Access(
                source,
                caller,
                local.Name,
                caller,
                false,
                field
            );
        var imported = (TupleTerm)identity;

        return NativeRecordLanguage.Access(
            source,
            ((Atom)imported.Items[0]).Name,
            ((Atom)imported.Items[1]).Name,
            caller,
            true,
            field
        );
    }

}
