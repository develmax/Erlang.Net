namespace Erlang;

internal sealed class PreparedHeapImport : IDisposable
{
    private ProcessTermFrame506? roots;
    private readonly Term value;

    internal PreparedHeapImport(ProcessTermFrame506 roots, Term value)
    {
        this.roots = roots;
        this.value = value;
    }

    internal Term Value => Volatile.Read(ref roots) is not null ? value : throw new ObjectDisposedException(nameof(PreparedHeapImport));

    public void Dispose() => Interlocked.Exchange(ref roots, null)?.Dispose();
}
