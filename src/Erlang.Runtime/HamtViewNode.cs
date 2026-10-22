namespace Erlang;

internal sealed class HamtViewNode
{
    internal KeyValuePair<Term, Term>? Leaf { get; }
    internal IReadOnlyList<HamtViewNode> Children { get; }
    internal ushort Bitmap { get; }
    internal bool IsCollision { get; }
    internal bool IsHead { get; }
    internal Term? StoredTerm { get; }

    internal HamtViewNode(
        KeyValuePair<Term, Term>? leaf,
        IReadOnlyList<HamtViewNode> children,
        ushort bitmap = 0,
        bool collision = false,
        object? domain = null,
        object? ownership = null,
        bool head = false
    )
    {
        Leaf = leaf;
        Children = Array.AsReadOnly(children.ToArray());
        Bitmap = bitmap;
        IsCollision = collision;
        IsHead = head;
        if (!head && domain is not null && ownership is not null)
            StoredTerm = leaf is { } pair ? new Cons(pair.Key, pair.Value)
                : collision ? new TupleTerm(Children.Select(child => child.StoredTerm!).ToArray())
                : new HamtBitmapTerm429(
                    domain,
                    ownership,
                    bitmap,
                    Children.Select(child => child.StoredTerm!)
                );
    }
}
