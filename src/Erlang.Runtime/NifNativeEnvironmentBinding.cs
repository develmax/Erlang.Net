namespace Erlang;

internal sealed class NifNativeEnvironmentBinding : IDisposable
{
    private NifNativeCallbackScope? owner;
    private NifNativeCallEnvironment? previous;

    internal NifNativeEnvironmentBinding(NifNativeCallbackScope scope, NifNativeCallEnvironment? previous)
    {
        owner = scope;
        this.previous = previous;
    }

    public void Dispose()
    {
        var scope = Interlocked.Exchange(ref owner, null);
        if (scope is null)
            return;
        scope.RestoreEnvironment(previous);
        previous = null;
    }
}
