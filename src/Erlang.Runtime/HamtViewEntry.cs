namespace Erlang;

internal sealed record HamtViewEntry(
    KeyValuePair<Term, Term> Pair,
    ulong Hash);
