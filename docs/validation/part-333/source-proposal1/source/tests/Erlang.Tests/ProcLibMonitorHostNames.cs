namespace Erlang.Tests;

internal static class ProcLibMonitorHostNames
{
    public const string Test = "host_proc_lib_monitored_startup_retains_reference_and_requeues_real_down_at_tail";
    public const string Method = "WaitMonitoredInitialization";
    public const string Failure = "Monitored startup reference and real DOWN queue-tail contract failed";
}
