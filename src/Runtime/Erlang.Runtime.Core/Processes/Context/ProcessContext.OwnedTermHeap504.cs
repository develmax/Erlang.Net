namespace Erlang;

public sealed partial class ProcessContext
{
    // Separate substrate: not yet the backing of guest Term construction.
    internal ProcessOwnedTermHeap504 OwnedTermHeap504 { get; } = new();
}
