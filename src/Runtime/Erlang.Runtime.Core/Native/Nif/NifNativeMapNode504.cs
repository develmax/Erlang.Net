namespace Erlang;

internal sealed class NifNativeMapNode504 : NifNativeTermNode
{
    internal IReadOnlyList<KeyValuePair<NifNativeTermValue, NifNativeTermValue>> Entries { get; }
    internal NifNativeMapBranch504? Root { get; }
    internal NifNativeMapHashRetention504? HashRetention { get; }
    internal ProcessRuntime Runtime { get; }

    internal NifNativeMapNode504(KeyValuePair<NifNativeTermValue, NifNativeTermValue>[] entries, ProcessRuntime runtime)
    {
        Entries = Array.AsReadOnly(entries);
        Runtime = runtime;
    }

    internal NifNativeMapNode504(NifNativeMapBranch504 root, NifNativeMapHashRetention504 retention, ProcessRuntime runtime)
    {
        Runtime = runtime;
        Root = root;
        HashRetention = retention;
        Entries = Array.AsReadOnly(root.Entries().ToArray());
    }

    internal NifNativeMapNode504 Copy(Func<NifNativeTermValue, NifNativeTermValue> read)
        => Root is { } root ? new(root.Copy(read), HashRetention!, Runtime)
            : new(
                Entries.Select(entry => new KeyValuePair<NifNativeTermValue, NifNativeTermValue>(read(entry.Key), read(entry.Value))).ToArray(),
                Runtime
            );

    internal override int ChildCount => checked(Entries.Count * 2);

    internal override NifNativeTermValue Child(int index)
        => index % 2 == 0 ? Entries[index / 2].Key : Entries[index / 2].Value;

    internal override Term Build(IReadOnlyDictionary<NifNativeTermNode, Term> cache)
        => NativeMapMaterialization505.Build(
            Runtime,
            Entries.Select(entry =>
            new KeyValuePair<Term, Term>(entry.Key.FromCache(cache), entry.Value.FromCache(cache))).ToArray(),
            Root,
            HashRetention,
            cache
        );
}
