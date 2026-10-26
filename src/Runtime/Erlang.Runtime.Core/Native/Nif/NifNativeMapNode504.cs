namespace Erlang;

internal sealed class NifNativeMapNode504 : NifNativeTermNode
{
    internal IReadOnlyList<KeyValuePair<NifNativeTermValue, NifNativeTermValue>> Entries { get; }
    internal NifNativeMapBranch504? Root { get; }
    internal NifNativeMapHashRetention504? HashRetention { get; }

    internal NifNativeMapNode504(KeyValuePair<NifNativeTermValue, NifNativeTermValue>[] entries)
    {
        Entries = Array.AsReadOnly(entries);
    }

    internal NifNativeMapNode504(NifNativeMapBranch504 root, NifNativeMapHashRetention504 retention)
    {
        Root = root;
        HashRetention = retention;
        Entries = Array.AsReadOnly(root.Entries().ToArray());
    }

    internal NifNativeMapNode504 Copy(Func<NifNativeTermValue, NifNativeTermValue> read)
        => Root is { } root ? new(root.Copy(read), HashRetention!)
            : new(Entries.Select(entry => new KeyValuePair<NifNativeTermValue, NifNativeTermValue>(read(entry.Key), read(entry.Value))).ToArray());

    internal override int ChildCount => checked(Entries.Count * 2);

    internal override NifNativeTermValue Child(int index)
        => index % 2 == 0 ? Entries[index / 2].Key : Entries[index / 2].Value;

    internal override Term Build(IReadOnlyDictionary<NifNativeTermNode, Term> cache)
        => MapTerm.FromNativeLayout504(Entries.Select(entry => new KeyValuePair<Term, Term>(entry.Key.FromCache(cache), entry.Value.FromCache(cache))).ToArray());
}
