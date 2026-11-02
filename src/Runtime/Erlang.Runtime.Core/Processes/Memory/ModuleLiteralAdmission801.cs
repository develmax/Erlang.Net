namespace Erlang;

internal sealed class ModuleLiteralAdmission801(Action restore) : IDisposable
{
    private Action? pending = restore;

    public void Dispose() => Interlocked.Exchange(ref pending, null)?.Invoke();
}
