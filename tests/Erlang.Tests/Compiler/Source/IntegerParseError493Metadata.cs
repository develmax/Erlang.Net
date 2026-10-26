namespace Erlang.Tests;

internal static class IntegerParseError493Metadata
{
    private const string File = "file";
    private const string Line = "line";
    private const string Source = "erlang.erl";
    private const int BadArgumentWithInfoLine = 13498;
    private const int ErrorWithInfoLine = 13495;

    internal static Term WithLocation(Term info, bool big = false) => Cons.From(new[]
    {
        Term.Tuple(Term.A(File), Term.String(Source)),
        Term.Tuple(Term.A(Line), Term.I(big ? ErrorWithInfoLine : BadArgumentWithInfoLine))
    }.Concat(Cons.Items(info)));
}
