namespace Erlang;

internal sealed record HamtViewNode(
    KeyValuePair<Term, Term>? Leaf,
    IReadOnlyList<HamtViewNode> Children);
