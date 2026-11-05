using System.Collections.Concurrent;

namespace Erlang;

internal sealed class AtomNameTable
{
    // Each entry keeps its registration ordinal and the canonical name instance
    // shared by every Atom with that name (and observed once as atom storage).
    private readonly ConcurrentDictionary<string, AtomNameEntry> names = new(StringComparer.Ordinal);
    private readonly object gate = new();

    private readonly Action exhausted;
    internal int Capacity { get; }

    internal AtomNameTable(int capacity, Action exhausted)
    {
        Capacity = capacity;
        this.exhausted = exhausted;
        names.TryAdd(ErlangBooleanAtoms.False, new(0, ErlangBooleanAtoms.False));
        ObservedPayloadStorage.Atom(ErlangBooleanAtoms.False);
        foreach (string name in ErlangPrimordialAtomNames.All)
        {
            names.TryAdd(name, new(names.Count, name));
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
                names.TryAdd(name, new(names.Count, name));
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

        return names[name].Name;
    }

    // The authoritative registration ordinal is already stored in names. No second
    // reverse cache or guessed OTP index is introduced by this diagnostic path.
    internal bool TryNameAt(int index, out string name)
    {
        lock (gate)
        {
            if ((uint)index < (uint)names.Count)
                foreach (var entry in names)
                    if (entry.Value.Index == index)
                    {
                        name = entry.Value.Name;

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

        return names[name].Index;
    }
}
