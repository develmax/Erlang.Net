namespace Erlang.NifNativeBackend;

internal static class CpuHostUserWork
{
    internal static async Task<CpuWorkMeasurement> Burn(TimeSpan targetCpu)
    {
        var result = new TaskCompletionSource<CpuWorkMeasurement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                result.TrySetResult(CpuUserTimeProbe.Burn(targetCpu));
            }
            catch (Exception error)
            {
                result.TrySetException(error);
            }
        })
        { IsBackground = true };
        thread.Start();
        try
        {
            return await result.Task.WaitAsync(CpuStatisticsParameters.Deadline);
        }
        finally
        {
            CpuStatisticsAccess.Require(thread.Join(CpuStatisticsParameters.Deadline));
        }
    }
}
