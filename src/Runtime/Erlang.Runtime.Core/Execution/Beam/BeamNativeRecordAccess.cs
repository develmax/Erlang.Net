namespace Erlang;

internal static class BeamNativeRecordAccess
{
    internal static bool GetElements(BeamLinkedCode code, BeamRegisterState registers, IReadOnlyList<BeamOperand> operands)
    {
        if (BeamRegisterInterpreter.Read(code, registers, operands[1]) is not NativeRecordTerm record)
            throw new InvalidDataException(BeamLinkDiagnostics.OperandRole);
        // A destination can overwrite the source register. Preserve the actual
        // record graph until all reads/writes complete, including partial writes
        // before a missing field, as in pinned erl_get_record_elements.
        using var roots = registers.HasOwnedRoots507 ? registers.Protect507(record) : null;
        for (int field = 3; field < operands.Count; field += 2)
        {
            string name = code.Structure.Atoms[(int)operands[field].Value]!;
            int index = -1;
            for (int candidate = 0; candidate < record.FieldNames.Count; candidate++)
                if (record.FieldNames[candidate].Name == name)
                {
                    index = candidate;
                    break;
                }
            if (index < 0) return false;
            registers.Write(operands[field + 1], record.Values[index]);
        }

        return true;
    }
}
