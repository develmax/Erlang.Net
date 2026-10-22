using System.Collections.Concurrent;

namespace Erlang;

public static class AtomNameRegistry
{
    private static readonly ConcurrentDictionary<string, int> names = new(StringComparer.Ordinal);
    private static readonly object gate = new();

    static AtomNameRegistry()
    {
        names.TryAdd(ErlangBooleanAtoms.False, 0);
        ObservedPayloadStorage.Atom(ErlangBooleanAtoms.False);
        foreach (string name in ErlangPrimordialAtomNames.All)
        {
            names.TryAdd(name, names.Count);
            ObservedPayloadStorage.Atom(name);
        }
    }

    public static void Observe(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (names.ContainsKey(name))
            return;
        AtomNameValidation.Validate(name);
        lock (gate)
        {
            if (names.ContainsKey(name))
                return;
            if (names.Count >= AtomTableLimits.MaximumNames)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            names.TryAdd(name, names.Count);
            ObservedPayloadStorage.Atom(name);
        }
    }

    public static string Register(string name)
    {
        Observe(name);
        ObservedPayloadStorage.Atom(name);

        return name;
    }

    // The authoritative registration ordinal is already stored in names. No second
    // reverse cache or guessed OTP index is introduced by this diagnostic path.
    internal static bool TryNameAt423(int index, out string name)
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

    public static int Count => names.Count;

    public static bool Exists(string name) => names.ContainsKey(name);

    public static int IndexOf(string name)
    {
        Observe(name);

        return names[name];
    }
}
