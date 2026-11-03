namespace Erlang;

internal sealed class PreparedHamtBitmapUse785(PreparedHamtBitmap785 owner) : IDisposable
{
    private PreparedHamtBitmap785? current = owner;

    internal bool IsFor(PreparedHamtBitmap785 owner) => ReferenceEquals(Volatile.Read(ref current), owner);

    public void Dispose() => Interlocked.Exchange(ref current, null)?.Leave();
}
