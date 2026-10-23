namespace Erlang;

internal sealed class EtsCursorBounds420(
    EtsCursorBoundness420 kind,
    Term? least,
    Term? most,
    IReadOnlyList<Term>? hashKeys
)
{
    internal EtsCursorBoundness420 Kind { get; } = kind;
    internal Term? Least { get; } = least;
    internal Term? Most { get; } = most;
    internal IReadOnlyList<Term>? HashKeys { get; } = hashKeys;
}
