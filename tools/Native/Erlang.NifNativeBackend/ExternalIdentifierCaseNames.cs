namespace Erlang.NifNativeBackend;

public static class ExternalIdentifierCaseNames
{
    public const string Physical = "runtime/statistics-external-identifiers-actual-node-header-birth-live-dead-reclaim";
    public const string Transfer = "runtime/statistics-external-identifiers-real-cross-heap-node-lease-host-escape";
    public const string Close = "runtime/statistics-external-identifiers-vm-close-real-node-drain";
    internal const string Node = "external_identifier@remote";
    internal const uint Creation = 0x89ABCDEF;
    internal const string Storage = "OwnedExternalStorage";
    internal const string PhysicalNodes = "physicalNodes";
    internal const ulong PidId = 0x1122334455667788;
    internal const ulong PortId = 0x8877665544332211;
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
