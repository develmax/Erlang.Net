using System.Reflection;

namespace Erlang.Tests;

internal static class MapHashRetentionHostNames
{
    internal const string ScalarTest = "HOST/map-hash404-scalar-repeated-epoch-identity";
    internal const string NestedTest = "HOST/map-hash404-normal-order-nested-fun-discovery";
    internal const string ForeignTest = "HOST/map-hash404-local-foreign-snapshot-ownership";
    internal const string ClosedTest = "HOST/map-hash404-close-rejects-new-hash-releases-old-lease";
    internal const string WeakTest = "HOST/map-hash404-weak-keys-identifier-only-churn";
    internal const string ConcurrentTest = "HOST/map-hash404-concurrent-scopes-exact-identity";
    internal const string PidTest = "HOST/map-hash404-held-noname-pid";
    internal const string PortTest = "HOST/map-hash404-held-noname-port";
    internal const string ReferenceTest = "HOST/map-hash404-held-noname-reference";
    internal const string FunctionTest = "HOST/map-hash404-held-noname-fun-environment";
    internal const string Remote = "map_hash404@remote";
    internal const string Local = "map_hash404@local";
    internal const string Foreign = "map_hash404_foreign@local";
    internal const string ChurnPrefix = "map_hash404_";
    internal const string ChurnSuffix = "@remote";
    internal const string Epochs = "mapHashEpochs";
    internal const string ActiveScopes = "ActiveScopes";
    internal const string Module = "map_hash404_host";
    internal const string EnvironmentKey = "retained_reference";
    internal const string Failure = "Map hash identifier ownership invariant failed.";
    internal const int FirstChurn = 256;
    internal const int SecondChurn = 65;
    internal const int ChurnBound = 128;
    internal const int ConcurrentCount = 32;
    internal const int UniqueBytes = 16;
    internal const uint LocalCreation = 37;
    internal const uint RemoteCreation = 1;
    internal const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
