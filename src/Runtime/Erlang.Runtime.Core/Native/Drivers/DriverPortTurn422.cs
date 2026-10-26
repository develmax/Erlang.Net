namespace Erlang;

// One actually dispatched normal-lane port execution. Source port scheduling
// starts at CONTEXT_REDS/4; this backend executes one callback per queued turn
// and yields the lane at completion. Charged work goes to real VM statistics.
internal sealed class DriverPortTurn422
{
    internal int Charged = ProcessSchedulingLimits.CooperativeSlice / 4;

    internal int Consume(int percent)
    {
        int slice = ProcessSchedulingLimits.CooperativeSlice;
        percent = Math.Clamp(percent, 1, 100);
        Charged = (int)Math.Min(slice, (long)Charged + percent * ((slice + 99) / 100));

        return Charged < slice ? 0 : 1;
    }
}
