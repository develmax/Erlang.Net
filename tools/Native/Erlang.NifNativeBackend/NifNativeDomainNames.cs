namespace Erlang.NifNativeBackend;

internal static class NifNativeDomainNames
{
    internal const string AllocationOwnerField = "allocations";
    internal const string VetoRole = "actual-native-load-veto-before-payload-allocation-domain-closed";
    internal const string LoadedRole = "actual-native-load-callback-payload-matches-library-owner-ledger";
    internal const string UnloadedRole = "actual-native-retirement-unload-releases-payload-and-domain-membership";
}
