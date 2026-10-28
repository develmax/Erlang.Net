namespace Erlang;

internal sealed class ProcessBinaryPublication512(
    ProcessBinaryFrame512 frame,
    int slot,
    ProcessBinaryLease512? previous,
    ProcessBinaryLease512 next
) : IDisposable
{
    private bool completed;

    internal void Commit512()
    {
        if (completed)
            throw new InvalidOperationException(BinaryPayload512Names.PublicationClosed);
        completed = true;
        previous?.Dispose();
    }

    public void Dispose()
    {
        if (completed)
            return;
        completed = true;
        frame.Rollback512(slot, previous);
        next.Dispose();
    }
}
