namespace Erlang;

internal static class ErlangInfallibleBifValues
{
    internal static Term Get(ProcessContext context, Term key)
        => context.Dictionary.GetValueOrDefault(key, Term.A(ProcessDictionaryAtoms.Undefined));

    internal static Term Minimum(Term first, Term second)
        => first.CompareTo(second) > 0 ? second : first;

    internal static Term Maximum(Term first, Term second)
        => first.CompareTo(second) < 0 ? second : first;
}
