using System.Numerics;

namespace Erlang;

// Charged cooperative guest work, never CLR allocations or fabricated physical VM metrics.
internal sealed class RuntimeReductionStatistics
{
    private readonly object gate = new();
    private BigInteger total;
    private BigInteger previousApproximate;
    private BigInteger previousExact;

    internal void Record(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0)
            return;
        lock (gate)
            total += count;
    }

    internal Term Sample(string key)
    {
        lock (gate)
        {
            BigInteger previous;
            if (key == RuntimeStatisticsNames.Reductions)
            {
                previous = previousApproximate;
                previousApproximate = total;
            }
            else if (key == RuntimeStatisticsNames.ExactReductions)
            {
                previous = previousExact;
                previousExact = total;
            }
            else
            {
                throw new ArgumentException(nameof(key));
            }

            return Term.Tuple(new Integer(total), new Integer(total - previous));
        }
    }
}
