namespace Erlang;

internal sealed class CodeTermComparer : IComparer<Term>
{
    internal static readonly CodeTermComparer Instance = new();

    public int Compare(Term? x, Term? y) => TermOrder.Compare(x!, y!);
}
