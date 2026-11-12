namespace Erlang;

internal static class SupervisorDefaults
{
    internal const int RestartIntensity = 1;
    internal const int RestartPeriodSeconds = 5;
    internal const int HibernateAfterMilliseconds = 1000;
    internal const int WorkerShutdownMilliseconds = 5000;
    internal const int DirtyRestartLimit = 1000;
}
