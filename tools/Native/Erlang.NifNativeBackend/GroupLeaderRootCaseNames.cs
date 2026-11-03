namespace Erlang.NifNativeBackend;

public static class GroupLeaderRootCaseNames
{
    public const string Storage = "runtime/statistics-group-leader-foreign-pid-physical-root-and-detached-host-snapshot";
    public const string Inheritance = "runtime/statistics-group-leader-spawn-inheritance-independent-physical-roots-and-close";
    internal const string Node = "group_leader_root@remote";
    internal const ulong Id = 0x1122334455667788;
    internal const uint Creation = 0x12345678;
    internal const nuint ExternalPidHeader = 0xF0;
    internal const int ExternalPidWords = 4;
    internal const string Failure = "Actual group leader process root/snapshot/inheritance/retirement contract failed";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
