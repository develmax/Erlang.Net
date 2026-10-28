namespace Erlang;

internal sealed class GuestAdoptionFrame507(Term source)
{
    internal Term Source { get; } = source;
    internal Term[] Children { get; } = source switch
    {
        Cons cons => [cons.Head, cons.Tail],
        TupleTerm tuple => tuple.Items.ToArray(),
        _ => []
    };
    internal Term[] Built { get; } = source switch
    {
        Cons => new Term[2],
        TupleTerm tuple => new Term[tuple.Items.Count],
        _ => []
    };
    internal int Next { get; set; }
}
