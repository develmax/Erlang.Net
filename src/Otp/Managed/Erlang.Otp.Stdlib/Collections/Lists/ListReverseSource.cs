namespace Erlang;

internal static class ListReverseSource
{
    internal const int Head = 292;
    internal const int NativeCall = 299;
    internal static readonly object Continuation = new();

    internal static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(ListFoldRightSource.FileName)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
    );
}
