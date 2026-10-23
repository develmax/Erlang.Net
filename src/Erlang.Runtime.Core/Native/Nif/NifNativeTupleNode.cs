namespace Erlang;

internal sealed class NifNativeTupleNode(NifNativeTermValue[] elements) : NifNativeTermNode
{
    internal IReadOnlyList<NifNativeTermValue> Elements => elements;
    internal override int ChildCount => elements.Length;

    internal override NifNativeTermValue Child(int index) => elements[index];

    internal override Term Build(IReadOnlyDictionary<NifNativeTermNode, Term> cache)
    {
        var terms = new Term[elements.Length];
        for (int index = 0; index < terms.Length; index++)
            terms[index] = elements[index].FromCache(cache);

        return Term.Tuple(terms);
    }
}
