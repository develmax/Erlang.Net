namespace Erlang;

internal sealed class NifNativeEnvironmentBinding : IDisposable
{
    private NifNativeCallbackScope? owner;
    private NifNativeCallEnvironment? previous;
    private readonly AtomNameScope atoms;

    internal NifNativeEnvironmentBinding(NifNativeCallbackScope scope, NifNativeCallEnvironment? previous, NifNativeCallEnvironment environment)
    {
        owner = scope;
        this.previous = previous;
        atoms = new AtomNameScope(environment.Runtime.AtomNames);
    }

    public void Dispose()
    {
        var scope = Interlocked.Exchange(ref owner, null);
        if (scope is null)
            return;
        scope.RestoreEnvironment(previous);
        atoms.Dispose();
        previous = null;
    }
}
