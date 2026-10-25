using System.Reflection;

namespace Erlang.NifNativeBackend;

internal static class NifNativeFenceNames
{
    internal const string OwnerGate = "gate";
    internal const string LifecycleSemaphore = "gate";
    internal const string ProviderContext = "nif-native-provider-refusal-control";
    internal const string GateType = "Erlang.NifNativeLifecycleGate";
    internal const string PlatformType = "Erlang.NifNativePlatformLoader";
    internal const string VerifyProvider = "RequireDefaultProvider";
    internal const string VerifyPlatform = "VerifyPlatform";
    internal const string Enter = "Enter";
    internal const string JoinFailure = "Owned native lifecycle control thread did not finish within its deadline.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    internal const int ProbeMilliseconds = 40;
    internal const int LoadInfo = 42;
    internal const int BothOwners = 2;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    internal const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.NonPublic;
}
