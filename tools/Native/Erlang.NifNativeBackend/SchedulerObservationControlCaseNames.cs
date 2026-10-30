namespace Erlang.NifNativeBackend;

public static class SchedulerObservationControlCaseNames
{
    public const string Native = "runtime/statistics-walltime-native-wait-services-owner-process-info";
    public const string Gather = "runtime/statistics-walltime-gather-wait-services-owner-process-info";
    public const string Managed = "runtime/statistics-walltime-managed-wait-reports-waiting-and-services-control";
    internal const int Capacity = 3;
    internal const int TargetLane = 1;
    internal const int QueryLane = 2;
    internal const int InspectorLane = 3;
}
