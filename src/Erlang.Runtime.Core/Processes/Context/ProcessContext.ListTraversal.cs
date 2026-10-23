namespace Erlang;

public sealed partial class ProcessContext
{
    private long cumulativeSuspensionTicks;

    // Read only by the executing actor, like ChargeAsync's reductions field.
    internal int RemainingListTraversalReductions => ProcessSchedulingLimits.CooperativeSlice - reductions;
}
