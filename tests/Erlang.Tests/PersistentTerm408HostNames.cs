using System.Reflection;

namespace Erlang.Tests;

internal static class PersistentTerm408HostNames
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal const string ValueTest = "HOST/persistent-term408/exact-values-and-owned-copy";
    internal const string LifetimeTest = "HOST/persistent-term408/process-independent-and-atomic-new";
    internal const string LeaseTest = "HOST/persistent-term408/retired-read-lease-and-close";
    internal const string Snapshot = "Snapshot";
    internal const string Materialize = "Materialize";
    internal const string Report = "OwnedStorageReport";
    internal const string Payloads = "RetainedPairPayloads";
    internal const string Property = "PersistentTerms";
    internal const string Put = "Put";
    internal const string Get = "Get";
    internal const string Erase = "Erase";
    internal const string Close = "CloseAsync";
    internal const string GetAll = "GetAll";
    internal const string Key = "persistent408_key";
    internal const string Shared = "persistent408_shared";
    internal const string Failure = "Persistent term408 owned storage control failed";
}
