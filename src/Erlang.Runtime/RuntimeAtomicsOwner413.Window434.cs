namespace Erlang;

internal sealed partial class RuntimeAtomicsOwner413
{
    internal AllocatorBlockWindow434[] CaptureWindow434()
    {
        AtomicsArray413[] published;
        lock (gate) published = arrays.Keys.ToArray();

        return published.Where(array => array.IsAllocated434).Select(array => new AllocatorBlockWindow434(
            AllocatorBackendNames.Standard,
            AllocatorGatherNames434.AtomicsType,
            Term.A(AllocatorGatherNames434.System),
            array.AllocatedBytes
        )).ToArray();
    }
}
