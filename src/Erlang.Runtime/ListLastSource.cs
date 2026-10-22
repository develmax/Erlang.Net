namespace Erlang;

internal static class ListLastSource
{
    internal const string File = "lists.erl";
    internal const int PublicClause = 467;
    internal const int HelperClause = 469;

    internal static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(File)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
    );
}
