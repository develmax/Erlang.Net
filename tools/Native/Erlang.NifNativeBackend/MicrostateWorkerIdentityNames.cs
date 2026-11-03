namespace Erlang.NifNativeBackend;

public static class MicrostateWorkerIdentityNames
{
    public const string Normal = "runtime/statistics-msacc-normal-physical-worker-id-out-of-order-birth";
    public const string Dirty = "runtime/statistics-msacc-dirty-io-physical-worker-id-out-of-order-birth";
    public const string Duplicate = "runtime/statistics-msacc-duplicate-id-distinct-physical-threads-rejected";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
