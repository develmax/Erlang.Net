using System.Reflection;

namespace Erlang.Tests;

internal static class AtomicsPersistentSelection591Names
{
    internal const string KeyCase = "runtime/atomics591/persistent-get-does-not-export-unused-atomics-key";
    internal const string AliasCase = "runtime/atomics591/persistent-value-selection-preserves-alias-and-host-custody";
    internal const string HostCarrier = "AtomicsHost586";
    internal const string NativeType = "Erlang.AtomicsNative413", Capture = "TryCapture", GuestModule = "atomics";
    internal const string Failure = "Persistent comparison or unused key retained a hidden atomics host lease.";
    internal const string CleanupFailure = "Persistent selection assertion and independent cleanup failed.";
    internal const int Value = 591;
    internal const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
}
