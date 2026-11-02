using System.Numerics;

namespace Erlang;

internal sealed partial class RuntimeStatistics
{
    private readonly object ioAndContextGate = new();
    private BigInteger inputBytes;
    private BigInteger outputBytes;
    private BigInteger selectedProcessDispatches;

    internal void RecordInputBytes(long count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0)
            return;

        lock (ioAndContextGate)
            inputBytes += count;
    }

    internal void RecordOutputBytes(long count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0)
            return;

        lock (ioAndContextGate)
            outputBytes += count;
    }

    internal void RecordSelectedProcessDispatch()
    {
        lock (ioAndContextGate)
            selectedProcessDispatches++;
    }

    internal Term SampleIo()
    {
        lock (ioAndContextGate)
            return Term.Tuple(
                Term.Tuple(Term.A(RuntimeStatisticsIoNames.Input), new Integer(inputBytes)),
                Term.Tuple(Term.A(RuntimeStatisticsIoNames.Output), new Integer(outputBytes))
            );
    }

    internal Term SampleContextSwitches()
    {
        lock (ioAndContextGate)
            return Term.Tuple(new Integer(selectedProcessDispatches), Term.I(0));
    }
}
