namespace Erlang;

internal sealed class AtomNameScope : IDisposable
{
    private static readonly AsyncLocal<AtomNameTable?> current = new();
    private readonly AtomNameTable? previous;

    internal static AtomNameTable? Current => current.Value;

    internal AtomNameScope(AtomNameTable table)
    {
        previous = current.Value;
        current.Value = table;
    }

    public void Dispose() => current.Value = previous;
}
