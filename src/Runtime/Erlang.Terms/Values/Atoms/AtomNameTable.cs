using System.Collections.Concurrent;

namespace Erlang;

internal sealed class AtomNameTable
{
    private readonly ConcurrentDictionary<string, int> names = new(StringComparer.Ordinal);
    private readonly object gate = new();

    private readonly Action exhausted;
    internal int Capacity { get; }

    internal AtomNameTable(int capacity, Action exhausted)
    {
        Capacity = capacity;
        this.exhausted = exhausted;
        names.TryAdd(ErlangBooleanAtoms.False, 0);
        ObservedPayloadStorage.Atom(ErlangBooleanAtoms.False);
        foreach (string name in ErlangPrimordialAtomNames.All)
        {
            names.TryAdd(name, names.Count);
            ObservedPayloadStorage.Atom(name);
        }
        if (capacity < names.Count)
            throw new ArgumentOutOfRangeException(nameof(capacity));
    }

    internal void Observe(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (names.ContainsKey(name))
            return;
        AtomNameValidation.Validate(name);
        lock (gate)
        {
            if (names.ContainsKey(name))
                return;
            if (names.Count < Capacity)
            {
                names.TryAdd(name, names.Count);
                ObservedPayloadStorage.Atom(name);

                return;
            }
        }
        // Shutdown can acquire runtime locks; release the table lock first.
        exhausted();
        throw new InvalidOperationException(nameof(AtomNameTable));
    }

    internal string Register(string name)
    {
        Observe(name);
        ObservedPayloadStorage.Atom(name);

        return name;
    }

    // The authoritative registration ordinal is already stored in names. No second
    // reverse cache or guessed OTP index is introduced by this diagnostic path.
    internal bool TryNameAt(int index, out string name)
    {
        lock (gate)
        {
            if ((uint)index < (uint)names.Count)
                foreach (var entry in names)
                    if (entry.Value == index)
                    {
                        name = entry.Key;

                        return true;
                    }
        }
        name = string.Empty;

        return false;
    }

    internal int Count => names.Count;

    internal bool Exists(string name) => names.ContainsKey(name);

    internal int IndexOf(string name)
    {
        Observe(name);

        return names[name];
    }
}
