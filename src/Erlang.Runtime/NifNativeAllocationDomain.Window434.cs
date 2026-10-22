namespace Erlang;

internal sealed partial class NifNativeAllocationDomain
{
    private readonly Dictionary<long, NifAllocationScanOwner434> scanOwners434 = new();

    internal void RegisterScanOwner434(long membership, NifNativeAllocationOwner owner, string module)
    {
        var row = new NifAllocationScanOwner434(membership, new(owner), module);
        lock (gate)
        {
            if (!owners.ContainsKey(membership)) throw new InvalidOperationException(AllocatorGatherNames434.Membership);
            scanOwners434.Add(membership, row);
        }
    }

    internal AllocatorBlockWindow434[] CaptureWindow434()
    {
        NifAllocationScanOwner434[] published;
        lock (gate) published = scanOwners434.Values.ToArray();
        var result = new List<AllocatorBlockWindow434>();
        // Never take owner.gate while holding domain.gate: ordinary allocation
        // commits in the opposite owner -> domain order.
        foreach (var row in published)
            if (row.Owner.TryGetTarget(out var owner)) result.AddRange(owner.CaptureWindow434(row.Membership, row.Module));

        return result.ToArray();
    }
}
