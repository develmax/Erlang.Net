namespace Erlang.NifNativeBackend;

internal static class CpuStatisticsParameters
{
    internal const string Failure = "Actual logical VM user CPU attribution contract failed";
    internal const string Unavailable = "Actual OS user CPU measurement unavailable; CPU compatibility gate remains unaccepted";
    internal const string DirtyModule = "cpu_statistics_control";
    internal const string DirtyFunction = "physical_work";
    internal const string DispatcherField = "actorDispatcher";
    internal const int SpinIterations = 4096;
    internal const int AccountingQuantaPerWork = 12;
    internal const int CollectionAttempts = 4;
    internal static readonly TimeSpan CalibrationCpu = TimeSpan.FromMilliseconds(20);
    internal static readonly TimeSpan MinimumWorkCpu = TimeSpan.FromMilliseconds(80);
    internal static readonly TimeSpan MaximumWorkCpu = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(8);
}
