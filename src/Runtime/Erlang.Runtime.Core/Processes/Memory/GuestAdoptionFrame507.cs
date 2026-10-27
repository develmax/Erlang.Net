namespace Erlang;

internal sealed class GuestAdoptionFrame507
{
    internal Term Source { get; }
    internal PreparedOwnedMap517? MapPlan { get; }
    internal PreparedOwnedMapNode526? NodePlan526 { get; }
    internal Term[] Children { get; }
    internal Term[] Built { get; }
    internal int Next { get; set; }

    internal GuestAdoptionFrame507(Term source, PreparedMapGraph517? maps)
    {
        Source = source;
        MapPlan = source is MapTerm map ? maps?.Find(map) : null;
        NodePlan526 = maps?.FindNode526(source);
        Children = NodePlan526 is { } nodePlan ? nodePlan.Children526()
            : MapPlan is { } plan ? plan.Children.ToArray() : source switch
            {
                Cons cons => [cons.Head, cons.Tail],
                TupleTerm tuple => tuple.Items.ToArray(),
                _ => []
            };
        Built = new Term[Children.Length];
    }
}
