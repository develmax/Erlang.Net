namespace Erlang;

internal abstract class NifNativeTermNode
{
    internal abstract int ChildCount { get; }

    internal abstract NifNativeTermValue Child(int index);

    internal abstract Term Build(IReadOnlyDictionary<NifNativeTermNode, Term> cache);
}
