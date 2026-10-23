using System.Diagnostics;
using System.Numerics;

namespace Erlang;

internal sealed class RuntimeStatistics
{
    private readonly RuntimeReductionStatistics reductions = new();
    private readonly object gate = new();
    private readonly long started = Stopwatch.GetTimestamp();
    private BigInteger previousWallClock;
    private BigInteger previousUserCpu;

    internal void RecordReductions(int count) => reductions.Record(count);

    public Term Sample(string key)
    {
        if (key is RuntimeStatisticsNames.Reductions or RuntimeStatisticsNames.ExactReductions)
            return reductions.Sample(key);

        lock (gate)
        {
            BigInteger total;
            BigInteger previous;
            if (key == RuntimeStatisticsNames.WallClock)
            {
                total = new BigInteger(Stopwatch.GetTimestamp() - started) * RuntimeStatisticsParameters.WallMillisecondsPerSecond / Stopwatch.Frequency;
                previous = previousWallClock;
                previousWallClock = total;
            }
            else
            {
                using var process = Process.GetCurrentProcess();
                total = process.UserProcessorTime.Ticks / RuntimeStatisticsParameters.UserCpuTicksPerMillisecond;
                previous = previousUserCpu;
                previousUserCpu = total;
            }

            return Term.Tuple(new Integer(total), new Integer(total - previous));
        }
    }
}
