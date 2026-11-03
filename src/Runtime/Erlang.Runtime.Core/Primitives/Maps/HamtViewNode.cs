namespace Erlang;

internal sealed partial class HamtViewNode
{
    private readonly KeyValuePair<Term, Term>? leaf;
    internal KeyValuePair<Term, Term>? Leaf => owned517 is { } owned ? owned.Leaf : leaf;
    private readonly IReadOnlyList<HamtViewNode> children;
    internal IReadOnlyList<HamtViewNode> Children => owned517 is { } owned ? owned.Children : children;
    private readonly ushort bitmap;
    internal ushort Bitmap => owned517 is { } owned ? owned.Header.Bitmap : bitmap;
    private readonly bool collision;
    internal bool IsCollision => owned517 is { } owned ? owned.Header.Kind == OwnedMapHeaderKind517.HamtCollision : collision;
    private readonly bool head;
    internal bool IsHead => owned517 is { } owned ? owned.Header.Kind == OwnedMapHeaderKind517.HamtHead : head;
    private readonly Term? storedTerm;
    internal Term? StoredTerm => owned517 is { } owned ? owned.StoredTerm : storedTerm;

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
        this.leaf = leaf;
        this.children = Array.AsReadOnly(children.ToArray());
        this.bitmap = bitmap;
        this.collision = collision;
        this.head = head;
        if (!head && domain is not null && ownership is not null)
            storedTerm = leaf is { } pair ? new Cons(pair.Key, pair.Value)
                : collision ? new TupleTerm(Children.Select(child => child.StoredTerm!).ToArray())
                : new HamtBitmapTerm429(
                    domain,
                    ownership,
                    bitmap,
                    Children.Select(child => child.StoredTerm!)
                );
    }
}
