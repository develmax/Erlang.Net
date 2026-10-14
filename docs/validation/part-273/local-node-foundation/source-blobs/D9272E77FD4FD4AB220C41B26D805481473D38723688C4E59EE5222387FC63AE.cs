namespace Erlang.Tests;

internal static class RuntimeNodeIdentityFoundationHostNames
{
    internal const string AllocationStatus = "Observed {0} bytes for 1024 warmed local/external identifier comparisons on one thread";
    internal const string ControlStatus = "PASS {0} local identity foundation controls; activation/admission not executed";
    internal const string Test = "runtime/local-node-identity-foundation-native-map-order";
    internal const string FirstNode = "0@localhost";
    internal const string ForeignNode = "a@localhost";
    internal const string RaceNode = "race@localhost";
    internal const string SnapshotProperty = "Snapshot";
    internal const string RehydratePid = "RehydratePid";
    internal const string RehydratePort = "RehydratePort";
    internal const string RehydrateReference = "Rehydrate";
    internal const string EncodingSnapshot = "NodeSnapshot";
    internal const string DecodeOwner = "NodeIdentity";
    internal const string SeedProperty = "LocalTermSeed";
    internal const string SecondNode = "z_identity@localhost";
    internal const string LocalProperty = "LocalIdentity";
    internal const string PublishMethod = "Publish";
    internal const string SelectMethod = "SelectObservedExternalEpochCreation";
    internal const string Encode = "term_to_binary";
    internal const string Decode = "binary_to_term";
    internal const string Local = "local";
    internal const string Phash2 = "phash2";
    internal const string Key = "key";
    internal const string Sent = "identity_alias_message";
    internal const string RetainedMapEquality = "Retained map equality differs from the observed original before/start/stop contract.";
    internal const string Failure = "Local node identity foundation assertion failed.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
