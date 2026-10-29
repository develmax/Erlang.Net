using System.Reflection;

namespace Erlang.Tests;

internal static class CountersDictionary596Names
{
    internal const string GetterCase = "runtime/counters596/public-dictionary-getter-host-retains-after-erase-gc";
    internal const string SnapshotCase = "runtime/counters596/public-dictionary-snapshot-host-retains-after-erase-gc";
    internal const string Key = "counters_host596";
    internal const string NativeType = "Erlang.CountersNative421";
    internal const string Capture = "TryCapture";
    internal const string GuestModule = "erts_internal";
    internal const string Put = "counters_put";
    internal const string Get = "counters_get";
    internal const string HostCarrier = "CountersHost596";
    internal const string Failure = "A public dictionary counters result did not retain its actual backing across erase and collection.";
    internal const string CleanupFailure = "Dictionary counters host export or independent cleanup failed.";
    internal const int StoredValue = 73;
    internal const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}
