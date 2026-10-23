namespace Erlang;

public sealed partial class ProcessContext
{
    internal int RemainingSlice419 => Math.Max(ProcessSchedulingLimits.CooperativeSlice - reductions, 1);
}
