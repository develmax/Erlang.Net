namespace Erlang.NifNativeBackend;

internal static class NifNativeHarnessNames
{
    internal const string TaintSecond = "nif_second_taint";
    internal const int SpillCount = 17;
    internal const string TupleProbe = "nif_bridge_probe_tuple";
    internal const string ListProbe = "nif_bridge_probe_list";
    internal const string EmptyTupleProbe = "nif_bridge_probe_empty_tuple";
    internal const string EmptyListProbe = "nif_bridge_probe_empty_list";
    internal const string PendingProbe = "nif_bridge_probe_pending";
    internal const string AbiPass = "Bounded native ABI cursor/pending checks passed; original OTP NOT executed by this harness.";
    internal const string Usage = "Use no arguments for managed checks; library-path trusted-library-sha [bridge-path trusted-bridge-sha [abi-probe-path trusted-probe-sha]] for native checks.";
    internal const string Broken = "NIF native backend prerequisite assertion failed.";
    internal const string ManagedPass = "Managed backend checks passed; native ABI and original OTP NOT executed.";
    internal const string NativePass = "Internal trusted native fixture checks passed; general NIF ABI NOT implemented.";
    internal const string UnloadCounter = "memory_allocator_fixture_unload_count";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    internal static readonly Atom FirstReason = Term.A("nif_first_reason");
    internal static readonly Atom LastReason = Term.A("nif_last_reason");
    internal static readonly Atom WrongInput = Term.A("nif_non_integer");
    internal const string HostError = "host boundary failure";
    internal const string Greeting = "native hello";
}
