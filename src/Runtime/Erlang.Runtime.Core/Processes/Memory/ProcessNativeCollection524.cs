namespace Erlang;

internal sealed class ProcessNativeCollection524(
    ProcessTermHeap506 owner,
    ProcessNativeHeader524[] dead,
    ProcessNativeHeader524[] survivors,
    bool major,
    long nextHighWater,
    long previousHighWater
)
{
    internal void Commit524() => owner.CommitNativeCollection524(
        dead,
        survivors,
        major,
        nextHighWater,
        previousHighWater
    );
}
