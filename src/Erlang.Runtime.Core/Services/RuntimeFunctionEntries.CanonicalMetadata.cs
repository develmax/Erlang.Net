namespace Erlang;

public sealed partial class RuntimeFunctionEntries
{
    private ModuleRegistry? sourceRegistry;

    internal void AttachSourceRegistry(ModuleRegistry registry)
    {
        ModuleRegistry? previous = Interlocked.CompareExchange(ref sourceRegistry, registry, null);
        if (previous is not null && !ReferenceEquals(previous, registry))
            throw new ArgumentException(RuntimeExternalTermDiagnostics.ForeignRegistry, nameof(registry));
    }

    // Must run before acquiring the function-entry gate. Code publication owns
    // the Registry gate and never needs the function-entry gate.
    private LocalFunctionIdentity CanonicalMetadata(LocalFunctionIdentity identity, int arity)
        => Volatile.Read(ref sourceRegistry)?.CanonicalFunctionIdentity(identity, arity) ?? identity;
}
