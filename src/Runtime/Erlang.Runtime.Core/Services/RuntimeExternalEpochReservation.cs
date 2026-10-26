namespace Erlang;

internal sealed class RuntimeExternalEpochReservation(Action release) : IDisposable
{
    private Action? release = release;

    public void Dispose()
    {
        Interlocked.Exchange(ref release, null)?.Invoke();
    }
}
