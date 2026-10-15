namespace Erlang;

public sealed partial class ProcessRuntime
{
    private NifNativeAllocationDomain? nativeAllocationDomain;

    internal NifNativeAllocationDomain NativeAllocationDomain
        => LazyInitializer.EnsureInitialized(ref nativeAllocationDomain);

    internal NifNativeAllocationDomainSnapshot NativeAllocationSnapshot
        => Volatile.Read(ref nativeAllocationDomain)?.Snapshot ?? default;
}
