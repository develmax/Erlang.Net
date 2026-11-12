namespace Erlang;

internal static class BeamReceivePureBifs
{
    internal static BeamExternalImport Import(BeamLinkedCode code, BeamInstruction instruction)
    {
        int index = instruction.Definition.Name switch
        {
            BeamLinkNames.Bif => 0,
            BeamGeneralOpcodeNames.UnaryBif or BeamGeneralOpcodeNames.BinaryBif => 1,
            BeamGeneralOpcodeNames.UnaryGcBif or BeamLinkNames.GuardBif => 2,
            _ => throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher)
        };
        BeamExternalImport import = BeamExternalImports.Resolve(code, (int)instruction.Operands[index].Value);
        bool accepted = import.Module == ErlangModuleNames.Module && (instruction.Definition.Name switch
        {
            BeamLinkNames.Bif => import.Function == ErlangModuleNames.Self && import.Arity == 0,
            BeamLinkNames.GuardBif => import.Function == ErlangOperators.Plus && import.Arity == 2,
            BeamGeneralOpcodeNames.BinaryBif => import.Function is ErlangModuleNames.Element or ErlangModuleNames.IsMapKey && import.Arity == 2,
            _ => (import.Function is ErlangProcessQueryNames.Node or ErlangModuleNames.ByteSize or ErlangModuleNames.TupleSize or ErlangModuleNames.Length
                or ErlangModuleNames.IsTuple or ErlangModuleNames.IsList or ErlangBasicBifNames.IsPort or ErlangOperators.Not) && import.Arity == 1
        });
        if (!accepted)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);

        return import;
    }

    internal static bool RequiresAsync(BeamLinkedCode code, BeamReceivePlan plan)
        => plan.MatcherInstructions.Any(index => code.Instructions[index].Definition.Name == BeamLinkNames.GuardBif
            || code.Instructions[index].Definition.Name is BeamGeneralOpcodeNames.UnaryBif or BeamGeneralOpcodeNames.UnaryGcBif
                && Import(code, code.Instructions[index]).Function == ErlangModuleNames.Length);

    internal static Term Unary(string function, Term value) => function switch
    {
        ErlangModuleNames.IsTuple => CoreModules.Boolean(value is TupleTerm),
        ErlangModuleNames.IsList => CoreModules.Boolean(value is Cons or Nil),
        ErlangBasicBifNames.IsPort => CoreModules.Boolean(value is PortTerm),
        ErlangOperators.Not => ErlangOperatorValues.Unary(function, value),
        ErlangProcessQueryNames.Node => ErlangProcessQueryRegistrations.NodeOf(value),
        ErlangModuleNames.ByteSize when value is BitString binary
            => Term.I((binary.BitLength + (long)BeamBinaryNames.BitsPerByte - 1) / BeamBinaryNames.BitsPerByte),
        ErlangModuleNames.ByteSize => throw new ErlangException(ErlangErrorReasons.BadArgument),
        ErlangModuleNames.TupleSize when value is TupleTerm tuple => Term.I(tuple.Items.Count),
        ErlangModuleNames.TupleSize => throw new ErlangException(ErlangErrorReasons.BadArgument),
        _ => throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher)
    };

    internal static Term Binary(string function, Term first, Term second) => function switch
    {
        ErlangModuleNames.Element => Element(first, second),
        ErlangModuleNames.IsMapKey when second is MapTerm map => CoreModules.Boolean(map.TryGet(first, out _)),
        ErlangModuleNames.IsMapKey => throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), second)),
        _ => throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher)
    };

    private static Term Element(Term index, Term value)
    {
        if (index is not Integer integer || value is not TupleTerm tuple || integer.Value <= 0 || integer.Value > tuple.Items.Count)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return tuple.Items[(int)integer.Value - 1];
    }
}
