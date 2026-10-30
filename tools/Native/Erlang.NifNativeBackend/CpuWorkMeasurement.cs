namespace Erlang.NifNativeBackend;

internal readonly record struct CpuWorkMeasurement(long UserTicks, long LargestObservedStepTicks)
{
    internal long Milliseconds => UserTicks / TimeSpan.TicksPerMillisecond;
}
