namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner
{
    // NEW adapter only. No owner body/locking or enif_system_info edits.
    internal NifNativeResourceImage524 ResourceAdapter524() => new(
        initializationContext.Runtime,
        image,
        allocations,
        () => Volatile.Read(ref privateData),
        () => reallocationEnabled,
        () => Volatile.Read(ref disposed)
    );
}
