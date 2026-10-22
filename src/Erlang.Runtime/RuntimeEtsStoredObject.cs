namespace Erlang;

internal sealed partial class RuntimeEtsStoredObject
{
    private readonly RuntimeEtsArrayAccounting accounting;
    private readonly object gate = new();
    private RuntimeEtsStoredPayload? payload;
    private int references = 1;

    private RuntimeEtsStoredObject(RuntimeEtsArrayAccounting accounting, RuntimeEtsStoredPayload payload)
    {
        this.accounting = accounting;
        this.payload = payload;
        accounting.Acquire(payload.ArrayPayloadBytes);
    }

    internal static RuntimeEtsStoredObject Capture(
        RuntimeEtsArrayAccounting accounting,
        ProcessRuntime owner,
        TupleTerm value,
        CancellationToken cancellation
    )
    {
        var payload = RuntimeEtsStoredPayload.Capture(owner, value, cancellation);
        try
        {
            return new(accounting, payload);
        }
        catch
        {
            payload.Dispose();
            throw;
        }
    }

    internal RuntimeEtsReadLease Acquire()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(payload is null, this);
            var lease = new RuntimeEtsReadLease(this);
            references = checked(references + 1);

            return lease;
        }
    }

    internal Term Materialize(CancellationToken cancellation)
    {
        // Every caller holds a read lease; the final release cannot race it.
        var owned = Volatile.Read(ref payload) ?? throw new ObjectDisposedException(nameof(RuntimeEtsStoredObject));

        return owned.Materialize(cancellation);
    }

    internal void Release()
    {
        RuntimeEtsStoredPayload? retired = null;
        lock (gate)
        {
            if (references <= 0) throw new InvalidOperationException();
            if (--references == 0) retired = Interlocked.Exchange(ref payload, null);
        }
        if (retired is null) return;
        try
        {
            retired.Dispose();
        }
        finally
        {
            accounting.Release(retired.ArrayPayloadBytes);
        }
    }
}
