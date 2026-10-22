namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal Term CoreMatch411(ProcessContext context, Term identifier, Term pattern)
    {
        // OTP ets_match_2 constructs exactly [{Pattern, [], ['$$']}].
        // CoreSelect owns table/access precedence, compilation, real snapshot
        // leases and ordered_set traversal; no callback or alternate matcher.
        Term specification = Term.List(Term.Tuple(
            pattern,
            Nil.Value,
            Term.List(Term.A(MatchSpecNames.Variables))
        ));

        return CoreSelect409(context, identifier, specification);
    }
}
