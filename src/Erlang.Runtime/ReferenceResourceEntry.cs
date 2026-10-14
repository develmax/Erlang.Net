namespace Erlang;

internal sealed class ReferenceResourceEntry
{
    private readonly object gate = new();
    private object? payload;

    public ReferenceResourceEntry(
        RuntimeReferenceResources owner,
        ReferenceTerm identity,
        ReferenceResourceKind kind,
        object payload
    )
    {
        Owner = owner;
        Identity = identity;
        Kind = kind;
        this.payload = payload;
    }

    public RuntimeReferenceResources Owner { get; }
    public ReferenceTerm Identity { get; }
    public ReferenceResourceKind Kind { get; }
    public bool Published { get; set; }

    public bool IsAlive
    {
        get
        {
            lock (gate)
                return payload is not null;
        }
    }

    public T Read<T>() where T : class
    {
        T result;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(payload is null, ReferenceResourceNames.Lease);
            result = (T)payload;
        }
        if (result is PreparedBeamImage { IsConsumed: true })
            throw new InvalidOperationException(ReferenceResourceNames.ConsumedPrepared);

        return result;
    }

    internal bool TryReadPreparedMetadata(out PreparedBeamImage? image)
    {
        lock (gate)
        {
            image = payload as PreparedBeamImage;

            return image is not null;
        }
    }

    public void Close()
    {
        lock (gate)
            payload = null;
    }
}
