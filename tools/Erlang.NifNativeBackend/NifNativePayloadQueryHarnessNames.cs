namespace Erlang.NifNativeBackend;

internal static class NifNativePayloadQueryHarnessNames
{
    internal const string Mode = "--native-payload-provider-query";
    internal const string Usage = "Use --native-payload-provider-query library-path trusted-library-sha [bridge-path trusted-bridge-sha].";
    internal const string QueryRole = "sdk-native-payload-provider-query";
    internal const string CompletedRole = "sdk-native-payload-provider-query-completed";
    internal const string Failure = "Native C payload provider query contract failed.";
    internal const string CleanupFailure = "NativePayloadQueryCleanupFailure";
    internal const int SchemaVersion = 1;
    internal const int LibraryArguments = 2;
    internal const int BridgeArguments = 4;
    internal const int LibraryPathIndex = 0;
    internal const int LibraryHashIndex = 1;
    internal const int BridgePathIndex = 2;
    internal const int BridgeHashIndex = 3;
    internal const int Instance = 0;
    internal const int LoadedOwners = 1;
    internal const int LoadedAllocations = 1;
    internal const int InformationIndex = 2;
    internal const int RequestedIndex = 0;
    internal const int BackingIndex = 1;
    internal const int ValueIndex = 1;
    internal const int VetoRuntime = 1;
    internal const int LoadedRuntime = 2;
}
