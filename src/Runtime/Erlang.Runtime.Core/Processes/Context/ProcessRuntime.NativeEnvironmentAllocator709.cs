namespace Erlang;

public sealed partial class ProcessRuntime
{
    private NifNativeVmEnvironmentAllocator709? independentEnvironmentAllocator709;

    internal NifNativeVmEnvironmentAllocator709 IndependentEnvironmentAllocator709
        => LazyInitializer.EnsureInitialized(
            ref independentEnvironmentAllocator709,
            () => new NifNativeVmEnvironmentAllocator709(this)
        );
}
