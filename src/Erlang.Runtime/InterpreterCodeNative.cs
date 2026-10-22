namespace Erlang;

internal static class InterpreterCodeNative
{
    // Pinned erl_bif_coverage.c / beam_bif_load.c, non-BEAMASM branches.
    // Our BEAM register interpreter has no BEAMASM coverage/debug tables.
    // This capability profile must not be reported as equivalent to the JIT oracle.
    internal static bool TryCapture(
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (module != InterpreterCodeNativeNames.Module)
            return false;
        if (function == InterpreterCodeNativeNames.CoverageSupport && arity == 0)
            body = (_, _) => ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.False));
        else if (InterpreterCodeNativeNames.BadArgumentSignatures.Contains((function, arity)))
            body = (_, _) => throw new ErlangException(ErlangErrorReasons.BadArgument);

        return body is not null;
    }
}
