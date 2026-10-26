namespace Erlang;

public static class AtomNameRegistry
{
    private static readonly AtomNameTable host = new(
        AtomTableLimits.MaximumNames,
        static () => throw new ErlangException(ErlangErrorReasons.SystemLimit)
    );

    internal static AtomNameTable Current => AtomNameScope.Current ?? host;

    public static void Observe(string name) => Current.Observe(name);

    public static string Register(string name) => Current.Register(name);

    internal static bool TryNameAt423(int index, out string name) => Current.TryNameAt(index, out name);

    public static int Count => Current.Count;

    public static bool Exists(string name) => Current.Exists(name);

    public static int IndexOf(string name) => Current.IndexOf(name);
}
