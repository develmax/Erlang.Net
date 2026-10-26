namespace Erlang;

internal static class BeamInfallibleBifContract
{
    internal static bool IsInfallible(BeamLinkedCode code, BeamInstruction instruction)
        => Function(code, instruction) is not null;

    internal static bool TryInvoke(
        ProcessContext context,
        BeamLinkedCode code,
        BeamInstruction instruction,
        IReadOnlyList<Term> arguments,
        out Term result
    )
    {
        string? function = Function(code, instruction);
        if (function is null)
        {
            result = null!;

            return false;
        }
        result = function switch
        {
            ErlangOperators.Less or ErlangOperators.LessOrEqual or ErlangOperators.Greater or ErlangOperators.GreaterOrEqual
                or ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual or ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual
                when arguments.Count == 2 => ErlangOperatorValues.Binary(
                    function,
                    arguments[0],
                    arguments[1],
                    context
                ),
            BeamInfallibleBifNames.TupleSize when arguments.Count == 1 && arguments[0] is TupleTerm tuple => Term.I(tuple.Items.Count),
            BeamInfallibleBifNames.Get when arguments.Count == 1 => ErlangInfallibleBifValues.Get(context, arguments[0]),
            BeamInfallibleBifNames.Minimum when arguments.Count == 2 => ErlangInfallibleBifValues.Minimum(arguments[0], arguments[1]),
            BeamInfallibleBifNames.Maximum when arguments.Count == 2 => ErlangInfallibleBifValues.Maximum(arguments[0], arguments[1]),
            _ => throw new InvalidDataException(BeamLinkDiagnostics.OperandRole)
        };

        return true;
    }

    private static string? Function(BeamLinkedCode code, BeamInstruction instruction)
    {
        int arity = instruction.Definition.Name switch
        {
            BeamGeneralOpcodeNames.UnaryBif => 1,
            BeamGeneralOpcodeNames.BinaryBif => 2,
            _ => 0
        };
        if (arity == 0) return null;
        BeamExternalImport import = BeamExternalImports.Resolve(code, (int)instruction.Operands[1].Value);
        if (import.Module != ErlangModuleNames.Module || import.Arity != arity) return null;

        return import.Function switch
        {
            ErlangOperators.Less or ErlangOperators.LessOrEqual or ErlangOperators.Greater or ErlangOperators.GreaterOrEqual
                or ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual or ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual
                when arity == 2 => import.Function,
            BeamInfallibleBifNames.TupleSize when arity == 1 && BeamGuardedTupleBifProof.HasTupleGuard(code, instruction) => import.Function,
            BeamInfallibleBifNames.Get when arity == 1 => import.Function,
            BeamInfallibleBifNames.Minimum or BeamInfallibleBifNames.Maximum when arity == 2 => import.Function,
            _ => null
        };
    }
}
