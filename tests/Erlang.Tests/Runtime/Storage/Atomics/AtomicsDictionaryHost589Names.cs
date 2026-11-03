using System.Reflection;

namespace Erlang.Tests;

internal static class AtomicsDictionaryHost589Names
{
    internal const string GetterCase = "runtime/atomics589/public-dictionary-getter-host-retains-after-erase-gc";
    internal const string SnapshotCase = "runtime/atomics589/public-dictionary-snapshot-host-retains-after-erase-gc";
    internal const string Key = "atomics_host589";
    internal const string NativeType = "Erlang.AtomicsNative413";
    internal const string Capture = "TryCapture";
    internal const string GuestModule = "atomics";
    internal const string Put = "put";
    internal const string Get = "get";
    internal const string HostCarrier = "AtomicsHost586";
    internal const string Failure = "A public dictionary atomics result did not retain its actual backing across erase and collection.";
    internal const string CleanupFailure = "Dictionary atomics host export or independent cleanup failed.";
    internal const int StoredValue = 73;
    internal const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}
