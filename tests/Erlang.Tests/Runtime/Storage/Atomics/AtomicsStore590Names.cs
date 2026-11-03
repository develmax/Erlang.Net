using System.Reflection;

namespace Erlang.Tests;

internal static class AtomicsStore590Names
{
    internal const string SendCase = "runtime/atomics590/queue-export-source-drop-destination-retirement";
    internal const string EtsCase = "runtime/atomics590/ets-domain-source-drop-destination-retirement";
    internal const string PersistentCase = "runtime/atomics590/persistent-domain-source-drop-destination-retirement";
    internal const string Key = "atomics_store590";
    internal const string Export = "Export";
    internal const string HostCarrier = "AtomicsHost586";
    internal const string NativeType = "Erlang.AtomicsNative413";
    internal const string Capture = "TryCapture";
    internal const string GuestModule = "atomics";
    internal const string Put = "put";
    internal const string Get = "get";
    internal const string Failure = "An actual atomics backing failed its queue/store custody transition.";
    internal const string CleanupFailure = "Atomics handoff assertions and independent cleanup failed.";
    internal const int ExpectedValue = 590;
    internal const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
}
