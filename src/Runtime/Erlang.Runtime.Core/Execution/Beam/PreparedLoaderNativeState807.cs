namespace Erlang;

// The actual native payload contains this state's GCHandle. Loader reads fetch
// that native payload and resolve it; no parallel CLR-only image field is used.
internal sealed class PreparedLoaderNativeState807(PreparedLoaderOwner807 owner, PreparedBeamImage image, ModuleLiteralLease800? literals)
{
    private ModuleLiteralLease800? literalLease = literals;
    internal bool HasLiteralLease807 => Volatile.Read(ref literalLease) is not null;

    internal void ReleaseLiterals807() => Interlocked.Exchange(ref literalLease, null)?.Dispose();

    internal PreparedLoaderOwner807 Owner807 { get; } = owner;
    internal PreparedBeamImage Image807 { get; } = image;
}
