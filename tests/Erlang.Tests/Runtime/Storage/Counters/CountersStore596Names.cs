using System.Reflection;

namespace Erlang.Tests;

internal static class CountersStore596Names
{
    internal const string SendCase = "runtime/counters596/queue-export-source-drop-destination-retirement";
    internal const string EtsCase = "runtime/counters596/ets-domain-source-drop-destination-retirement";
    internal const string PersistentCase = "runtime/counters596/persistent-domain-source-drop-destination-retirement";
    internal const string Key = "counters_store596";
    internal const string Export = "Export";
    internal const string HostCarrier = "CountersHost596";
    internal const string NativeType = "Erlang.CountersNative421";
    internal const string Capture = "TryCapture";
    internal const string GuestModule = "erts_internal";
    internal const string Put = "counters_put";
    internal const string Get = "counters_get";
    internal const string Failure = "An actual counters backing failed its queue/store custody transition.";
    internal const string CleanupFailure = "Counters handoff assertions and independent cleanup failed.";
    internal const int ExpectedValue = 590;
    internal const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
}
