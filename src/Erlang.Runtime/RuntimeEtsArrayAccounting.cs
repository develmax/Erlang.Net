namespace Erlang;

internal sealed class RuntimeEtsArrayAccounting
{
    private readonly object gate = new();
    private long ownedPayloads;
    private long ownedArrayPayloadBytes;

    internal void Acquire(long bytes)
    {
        lock (gate)
        {
            long count = checked(ownedPayloads + 1), total = checked(ownedArrayPayloadBytes + bytes);
            ownedPayloads = count;
            ownedArrayPayloadBytes = total;
        }
    }

    internal void Release(long bytes)
    {
        lock (gate)
        {
            if (ownedPayloads <= 0 || bytes > ownedArrayPayloadBytes)
                throw new InvalidOperationException();
            ownedPayloads--;
            ownedArrayPayloadBytes -= bytes;
        }
    }

    internal (long Payloads, long ArrayPayloadBytes) Snapshot()
    {
        lock (gate)
            return (ownedPayloads, ownedArrayPayloadBytes);
    }
}
