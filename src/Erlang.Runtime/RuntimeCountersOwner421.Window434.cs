namespace Erlang;

internal sealed partial class RuntimeCountersOwner421
{
    internal AllocatorBlockWindow434[] CaptureWindow434()
    {
        CountersArray421[] published;
        lock (gate) published = arrays.Keys.ToArray();

        return published.Where(array => array.IsAllocated434).Select(array => new AllocatorBlockWindow434(
            AllocatorBackendNames.Standard,
            AllocatorGatherNames434.CountersType,
            Term.A(AllocatorGatherNames434.System),
            array.AllocatedBytes
        )).ToArray();
    }
}
