namespace Erlang;

internal static class EtsQueryText
{
    internal const string LookupExpression = "~w:lookup(~w, ~w)";
    internal const string LookupManyExpression = "lists:flatmap(fun(V) -> ~w:lookup(~w, V) end, ~w)";
}
