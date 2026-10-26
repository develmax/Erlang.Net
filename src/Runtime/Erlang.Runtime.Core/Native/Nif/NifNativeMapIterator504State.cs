namespace Erlang;

internal sealed class NifNativeMapIterator504State
{
    private readonly object gate = new();
    private readonly nuint[] keys;
    private readonly nuint[] values;

    internal nuint Map { get; }
    internal IReadOnlyList<KeyValuePair<NifNativeTermValue, NifNativeTermValue>> Entries { get; }

    internal NifNativeMapIterator504State(
        nuint map,
        IReadOnlyList<KeyValuePair<NifNativeTermValue, NifNativeTermValue>> entries
    )
    {
        Map = map;
        Entries = Array.AsReadOnly(entries.ToArray());
        keys = new nuint[entries.Count];
        values = new nuint[entries.Count];
    }

    internal (nuint Key, nuint Value) Pair(NifNativeCallEnvironment owner, int position)
    {
        lock (gate)
        {
            if (keys[position] == 0)
            {
                nuint key = owner.AddValue(Entries[position].Key);
                nuint value = owner.AddValue(Entries[position].Value);
                keys[position] = key;
                values[position] = value;
            }

            return (keys[position], values[position]);
        }
    }
}
