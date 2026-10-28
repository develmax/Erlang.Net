namespace Erlang;

public sealed class RuntimeExternalTermReservation : IDisposable
{
    private Term? value;
    private IDisposable? native524;

    internal RuntimeExternalTermReservation(Term value)
    {
        native524 = NativeResourceRoots524.RetainGraph524(value);
        this.value = value;
    }

    public Term Value => Volatile.Read(ref value) ?? throw new ObjectDisposedException(nameof(RuntimeExternalTermReservation));

    public void Dispose()
    {
        Interlocked.Exchange(ref value, null);
        Interlocked.Exchange(ref native524, null)?.Dispose();
    }
}
