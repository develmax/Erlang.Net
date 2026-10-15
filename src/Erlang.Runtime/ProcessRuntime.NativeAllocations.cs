namespace Erlang;

public sealed partial class ProcessRuntime
{
    private NifNativeAllocationDomain? nativeAllocationDomain;

    internal NifNativeAllocationDomain NativeAllocationDomain
        => LazyInitializer.EnsureInitialized(ref nativeAllocationDomain);

    internal NifNativeAllocationDomainSnapshot NativeAllocationSnapshot
        => Volatile.Read(ref nativeAllocationDomain)?.Snapshot ?? default;

    internal ValueTask<NifNativeAllocationDomainSnapshot> CaptureNativeAllocationsAsync(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        NifNativeAllocationDomain? domain = Volatile.Read(ref nativeAllocationDomain);

        return domain is null ? new(default(NifNativeAllocationDomainSnapshot)) : domain.CaptureAsync(cancellation);
    }
}
