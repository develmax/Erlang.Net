namespace Erlang;

internal sealed class NifNativeConsNode(NifNativeTermValue head, NifNativeTermValue tail) : NifNativeTermNode
{
    internal NifNativeTermValue Head => head;
    internal NifNativeTermValue Tail => tail;
    internal override int ChildCount => 2;

    internal override NifNativeTermValue Child(int index) => index == 0 ? head : tail;

    internal override Term Build(IReadOnlyDictionary<NifNativeTermNode, Term> cache)
        => new Cons(head.FromCache(cache), tail.FromCache(cache));
}
