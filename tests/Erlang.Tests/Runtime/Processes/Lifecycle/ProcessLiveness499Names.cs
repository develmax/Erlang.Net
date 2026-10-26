namespace Erlang.Tests;

internal static class ProcessLiveness499Names
{
    internal const string Test = "runtime/process-liveness499/full-values-stacks";
    internal const string OrderingTest = "runtime/process-liveness499/exit-signal-order";
    internal const string Prefix = "Erlang.Tests.ProcessLiveness499.";
    internal const string Failure = "Process liveness differs from pinned OTP";
    internal const string Manifest = "manifest.json";
    internal const string ManifestSha256 = "43424F0FB1542BE67D65166E6EB20BFDC3936EED603337619ACF64AD80A4BED3";
    internal const string BeamExtension = ".beam";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string Separator = ".";
    internal const string Erlang = "erlang";
    internal const string PersistentTerm = "persistent_term";
    internal const string Alive = "is_process_alive";
    internal const string ProcessFlag = "process_flag";
    internal const string Scheduler = "scheduler";
    internal const string Kill = "kill";
    internal const string Exit = "exit";
    internal const string Normal = "normal";
    internal static readonly TimeSpan PendingWindow = TimeSpan.FromMilliseconds(100);
    internal const string Killed = "killed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
