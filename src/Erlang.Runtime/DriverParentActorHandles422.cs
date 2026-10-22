namespace Erlang;

// Stable own wire handles represent genuine immutable PID values. They never
// retain ProcessContext, guest arguments, source frames or native pointers.
internal sealed class DriverParentActorHandles422 : IDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<Pid, ulong> values = new();
    private readonly Dictionary<ulong, Pid> reverse = new();
    private ulong next;
    private bool closed;

    internal ulong Get(Pid identity)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (values.TryGetValue(identity, out ulong value)) return value;
            value = checked(next + 1);
            values.Add(identity, value);
            try
            {
                reverse.Add(value, identity);
            }
            catch
            {
                values.Remove(identity);
                throw;
            }
            next = value;

            return value;
        }
    }

    internal Pid Resolve(ulong handle)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);

            return reverse.GetValueOrDefault(handle)
                ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (closed) return;
            closed = true;
            values.Clear();
            reverse.Clear();
        }
    }
}
