namespace Erlang;

internal sealed class ReadonlyTraceBuildFrame817
{
    internal Term Source { get; }
    internal Term[] Children { get; }
    internal Term[] Built { get; }
    internal int Index { get; set; }

    internal ReadonlyTraceBuildFrame817(Term source)
    {
        Source = source;
        Children = source switch
        {
            Cons cons => [cons.Head, cons.Tail],
            TupleTerm tuple => tuple.Items.ToArray(),
            _ => []
        };
        Built = new Term[Children.Length];
    }
}
