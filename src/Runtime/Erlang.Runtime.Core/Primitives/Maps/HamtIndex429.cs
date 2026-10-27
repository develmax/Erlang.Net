namespace Erlang;

internal sealed class HamtIndex429
{
    internal HamtViewNode Root { get; }
    internal HamtHashLeases429 Ownership { get; }
    internal int Count { get; }
    internal object? RetainedOwnership { get; }

    internal HamtIndex429(
        HamtViewNode root,
        HamtHashLeases429 ownership,
        int count,
        object? retainedOwnership = null
    )
    {
        Root = root;
        Ownership = ownership;
        Count = count;
        RetainedOwnership = retainedOwnership;
    }
}
