namespace Erlang;

internal sealed class HamtIndex429
{
    internal HamtViewNode Root { get; }
    internal HamtHashLeases429 Ownership { get; }
    internal int Count { get; }

    internal HamtIndex429(HamtViewNode root, HamtHashLeases429 ownership, int count)
    {
        Root = root;
        Ownership = ownership;
        Count = count;
    }
}
