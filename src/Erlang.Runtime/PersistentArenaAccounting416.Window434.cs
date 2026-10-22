namespace Erlang;

internal sealed partial class PersistentArenaAccounting416
{
    private readonly Dictionary<nint, (PersistentAllocationKind416 Kind, long Bytes)> owned434 = new();

    internal AllocatorBlockWindow434[] CaptureWindow434()
    {
        lock (gate) return owned434.Values.Select(block => new AllocatorBlockWindow434(
            block.Kind == PersistentAllocationKind416.Pair ? AllocatorBackendNames.Literal : AllocatorBackendNames.LongLived,
            block.Kind == PersistentAllocationKind416.Pair ? AllocatorGatherNames434.LiteralType : AllocatorGatherNames434.PersistentType,
            Term.A(AllocatorGatherNames434.System),
            checked((ulong)block.Bytes)
        )).ToArray();
    }
}
