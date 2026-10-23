namespace Erlang;

public sealed partial class ProcessContext
{
    internal int DebugRemainingReductions419 => ProcessSchedulingLimits.CooperativeSlice - reductions;

    internal void DebugSetRemainingReductions419(int value) => reductions = ProcessSchedulingLimits.CooperativeSlice - value;
}
