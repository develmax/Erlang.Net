namespace Erlang;

internal static class ListFoldRightSource
{
    internal const string FileName = "lists.erl";
    internal const string Helper = "foldr_1";
    internal const int CallbackArity = 2;
    internal const int HelperArity = 3;
    internal const int PublicClause = 2499;
    internal const int HelperClause = 2502;
    internal const int RecursiveReturn = 2503;

    internal static readonly object RecursiveContinuation = new();

    internal static readonly Term PublicLocation = Location(PublicClause);
    internal static readonly Term HelperLocation = Location(HelperClause);
    internal static readonly Term ReturnLocation = Location(RecursiveReturn);

    private static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(FileName)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
    );
}
