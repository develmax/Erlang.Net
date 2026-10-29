namespace Erlang;

internal sealed class BinarySearchPatternHandle : IDisposable
{
    private ReferenceResourceLease<BinarySearchPattern>? resource;
    private BinaryArenaReader598? reader;
    // Source wrapper must stay live through reader admission and matching.
    private ReferenceTerm? source;
    internal BinarySearchPattern Pattern { get; }
    internal BinaryArenaReader598 Reader598 => reader ?? throw new ObjectDisposedException(nameof(BinarySearchPatternHandle));

    internal BinarySearchPatternHandle(
        BinarySearchPattern pattern,
        BinaryArenaReader598 reader,
        ReferenceResourceLease<BinarySearchPattern>? resource = null,
        ReferenceTerm? source = null
    )
    {
        Pattern = pattern;
        this.reader = reader;
        this.resource = resource;
        this.source = source;
    }

    public void Dispose()
    {
        try
        {
            Interlocked.Exchange(ref reader, null)?.Dispose();
        }
        finally
        {
            try
            {
                Interlocked.Exchange(ref resource, null)?.Dispose();
            }
            finally
            {
                GC.KeepAlive(Interlocked.Exchange(ref source, null));
            }
        }
    }
}
