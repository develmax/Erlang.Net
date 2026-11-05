namespace Erlang;

internal static class DetsText
{
    internal const string FreeListEnd = "END OF FREE LIST \n";
    internal const string FreeListStart = "FREE LIST ...... \n";
    internal const string PrintedTerm = "~p~n";
    internal const string QueryLookup = "~w:lookup(~w, ~w)";
    internal const string QueryLookupKeys = "lists:flatmap(fun(V) -> ~w:lookup(~w, V) end, ~w)";
    internal const string Segment = "<~w>SEGMENT ~w~n";
    internal const string Slot = "   <~w>~w: <~w:~p>~w~n";
    internal const string UnexpectedMessage = "** dets: unexpected message (ignored): ~tw~n";
}
