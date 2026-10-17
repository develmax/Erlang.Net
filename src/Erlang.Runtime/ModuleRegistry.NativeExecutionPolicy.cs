namespace Erlang;

public sealed partial class ModuleRegistry
{
    private Func<string, NifNativeReviewedBasicAbi?>? nativeExecutionPolicy;

    // Embedding policy grants exact image/ABI execution; it does not select module/export semantics.
    internal void InstallReviewedNativeExecutionPolicy(Func<string, NifNativeReviewedBasicAbi?> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        lock (gate)
        {
            if (nativeExecutionPolicy is not null)
                throw new InvalidOperationException(NifNativeDefaultLoaderNames.AlreadyConfigured);
            Volatile.Write(ref nativeExecutionPolicy, policy);
        }
    }
}
