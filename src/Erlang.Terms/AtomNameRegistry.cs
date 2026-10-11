using System.Collections.Concurrent;

namespace Erlang;

public static class AtomNameRegistry
{
    private static readonly ConcurrentDictionary<string, byte> names = new(StringComparer.Ordinal);
    private static readonly object gate = new();

    static AtomNameRegistry()
    {
        foreach (string name in ErlangPrimordialAtomNames.All)
            names.TryAdd(name, 0);
    }

    public static void Observe(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (names.ContainsKey(name))
            return;
        lock (gate)
        {
            if (names.ContainsKey(name))
                return;
            if (names.Count >= AtomTableLimits.MaximumNames)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            names.TryAdd(name, 0);
        }
    }

    public static string Register(string name)
    {
        Observe(name);

        return name;
    }

    public static int Count => names.Count;

    public static bool Exists(string name) => names.ContainsKey(name);
}
