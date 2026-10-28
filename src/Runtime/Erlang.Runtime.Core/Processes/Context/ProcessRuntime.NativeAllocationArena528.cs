namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly object nativeArenaFinalGate528 = new();

    private void FinalizeNativeAllocationArena528(bool nativeImagesSettled)
    {
        lock (nativeArenaFinalGate528)
        {
            NifNativeAllocationDomain? arena = Volatile.Read(ref nativeAllocationDomain);
            if (arena?.Finalized528 == true) return;
            NativeResourceDomain524? resources = Volatile.Read(ref nativeResources524);
            if (!nativeImagesSettled || resources is not null && !resources.PhysicallyRetired528)
                throw new InvalidOperationException(NifNativeAllocationArena528Names.PhysicalClosure);
            arena?.ReclaimFinal528();
        }
    }
}
