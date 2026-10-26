namespace Erlang;

internal static class BeamRecordMetadata
{
    internal static IReadOnlyList<NativeRecordDefinition> Read(BeamLinkedCode code)
    {
        BeamChunk? chunk = code.Structure.Chunks.LastOrDefault(value => value.Id == BeamPreparationNames.RecordsChunk);
        if (chunk is null || chunk.Payload.Length == 0)
            return [];
        var cursor = new BeamCursor(chunk.Payload);
        Require(cursor.ReadInt32() == BeamRecordMetadataNames.Version);
        int count = cursor.ReadInt32();
        cursor.ReadInt32(); // The native reader also treats the allocation hint separately from entries.
        Require(count >= 0 && count <= cursor.Remaining);
        byte[] payload = [.. cursor.Read(cursor.Remaining).Span, BeamOperandParameters.EndOfCodeOpcode];
        BeamStructure structure = code.Structure with { Code = code.Structure.Code with { Instructions = payload } };
        IReadOnlyList<BeamInstruction> entries = BeamInstructionReader.Read(structure);
        Require(entries.Count == count + 1);
        var definitions = new List<NativeRecordDefinition>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (BeamInstruction entry in entries.Take(count))
        {
            IReadOnlyList<BeamOperand> operands = entry.Operands;
            Require(entry.Definition.Name == BeamRecordMetadataNames.Entry
                && operands.Count >= BeamRecordMetadataNames.EntryOperands
                && operands[0].Kind == BeamOperandParameters.Atom
                && operands[1].Kind == BeamOperandParameters.Atom
                && operands[2].Kind == BeamOperandParameters.Count
                && operands[2].Value == operands.Count - BeamRecordMetadataNames.EntryOperands
                && operands[2].Value % BeamRecordMetadataNames.FieldOperands == 0);
            string name = code.Structure.Atoms[(int)operands[0].Value]!;
            string exported = code.Structure.Atoms[(int)operands[1].Value]!;
            Require(names.Add(name) && exported is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False);
            var fields = new List<KeyValuePair<Atom, Term?>>();
            for (int index = BeamRecordMetadataNames.EntryOperands; index < operands.Count; index += BeamRecordMetadataNames.FieldOperands)
            {
                Require(operands[index].Kind == BeamOperandParameters.Atom);
                Atom field = Term.A(code.Structure.Atoms[(int)operands[index].Value]!);
                BeamOperand value = operands[index + 1];
                Term? fallback = value.Kind switch
                {
                    BeamOperandParameters.Unsigned when value.Value == 0 => null,
                    BeamOperandParameters.Atom => Term.A(code.Structure.Atoms[(int)value.Value]!),
                    BeamOperandParameters.Nil => Nil.Value,
                    BeamOperandParameters.Integer => code.IntegerConstant(value.Value),
                    BeamOperandParameters.LiteralValue => code.Structure.Literals[(int)value.Value],
                    _ => throw new InvalidDataException(BeamRecordMetadataNames.Invalid)
                };
                fields.Add(new(field, fallback));
            }
            Require(fields.Select(field => field.Key).Distinct().Count() == fields.Count);
            definitions.Add(new(name, exported == ErlangBooleanAtoms.True, fields));
        }

        return definitions.AsReadOnly();
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException(BeamRecordMetadataNames.Invalid);
    }
}
