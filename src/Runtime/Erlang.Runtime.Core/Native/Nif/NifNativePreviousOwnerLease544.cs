namespace Erlang;

internal sealed class NifNativePreviousOwnerLease544(
    NifImageBindings source,
    NifResourceImagePin524 pin,
    ProcessRuntime runtime,
    ModuleCodeImage nextImage
) : IDisposable
{
    private readonly object gate544 = new();
    private NifNativeLibraryOwner? previous544;

    // Called once, under the source binding Ready gate, after the real pin admits.
    internal void Admit544(NifNativeLibraryOwner previous) => previous544 = previous;

    internal NifResourceImagePin524 RetainCallback544(
        NifNativeLibraryOwner candidate,
        out NifNativeLibraryOwner previous
    )
    {
        lock (gate544)
        {
            if (previous544 is not { } owner || !pin.IsActiveFor524(source)
                || !candidate.MatchesNextOwner544(runtime, nextImage))
                throw new InvalidOperationException(NifNativeUpgrade544Names.PreviousUnavailable);
            // The inherited fork is real physical custody, including after logical
            // retirement. It does not admit the upgrade transaction itself.
            NifResourceImagePin524 callback = pin.Fork524();
            previous = owner;

            return callback;
        }
    }

    public void Dispose()
    {
        lock (gate544)
        {
            if (previous544 is null) return;
            previous544 = null;
        }
        pin.Dispose();
    }
}
