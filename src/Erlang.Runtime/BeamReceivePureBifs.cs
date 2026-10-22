namespace Erlang;

internal static class BeamReceivePureBifs
{
    internal static BeamExternalImport Import(BeamLinkedCode code, BeamInstruction instruction)
    {
        int index = instruction.Definition.Name switch
        {
            BeamLinkNames.Bif => 0,
            BeamGeneralOpcodeNames.UnaryBif or BeamGeneralOpcodeNames.BinaryBif => 1,
            BeamGeneralOpcodeNames.UnaryGcBif => 2,
            _ => throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher)
        };
        BeamExternalImport import = BeamExternalImports.Resolve(code, (int)instruction.Operands[index].Value);
        bool accepted = import.Module == ErlangModuleNames.Module && (instruction.Definition.Name switch
        {
            BeamLinkNames.Bif => import.Function == ErlangModuleNames.Self && import.Arity == 0,
            BeamGeneralOpcodeNames.BinaryBif => import.Function == ErlangModuleNames.Element && import.Arity == 2,
            _ => (import.Function is ErlangProcessQueryNames.Node or ErlangModuleNames.ByteSize) && import.Arity == 1
        });
        if (!accepted)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);

        return import;
    }

    internal static Term Unary(string function, Term value) => function switch
    {
        ErlangProcessQueryNames.Node => ErlangProcessQueryRegistrations.NodeOf(value),
        ErlangModuleNames.ByteSize when value is BitString binary
            => Term.I((binary.BitLength + (long)BeamBinaryNames.BitsPerByte - 1) / BeamBinaryNames.BitsPerByte),
        ErlangModuleNames.ByteSize => throw new ErlangException(ErlangErrorReasons.BadArgument),
        _ => throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher)
    };

    internal static Term Element(Term index, Term value)
    {
        if (index is not Integer integer || value is not TupleTerm tuple || integer.Value <= 0 || integer.Value > tuple.Items.Count)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return tuple.Items[(int)integer.Value - 1];
    }
}
