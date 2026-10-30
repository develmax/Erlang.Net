namespace Erlang.NifNativeBackend;

internal sealed class CpuMeasurementUnavailableException(Exception? cause = null)
    : InvalidOperationException(CpuStatisticsParameters.Unavailable, cause);
