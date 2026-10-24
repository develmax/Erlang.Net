using System.Reflection;
namespace Erlang.Tests;

internal static class DebugLocks419Names
{
    internal const string SourceTest = "HOST/erts-debug419/no-lock-counting-real-source4-and-fullargs", OwnerTest = "HOST/erts-debug419/no-lock-counting-owner-cancel-close";
    internal const string Module = "erts_debug", NativeType = "Erlang.DebugLockNative419", Capture = "TryCapture", Stop = "Stop", Resource = "Erlang.Tests.DebugLocks419.erts_debug.beam", Manifest = "Erlang.Tests.DebugLocks419.manifest.json", Failure = "Lock-counting-disabled419 source-profile control failed";
    internal const string Runtime = "erlang", SystemInfo = "system_info", LockCounting = "lock_counting", False = "false", Error = "error", Control = "lcnt_control", Clear = "lcnt_clear", Collect = "lcnt_collect";
    internal const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
    internal static readonly (string Function, int Arity)[] Signatures = [(Clear, 0), (Collect, 0), (Control, 1), (Control, 2)];
}
