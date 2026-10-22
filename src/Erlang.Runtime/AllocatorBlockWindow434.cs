namespace Erlang;
// Metadata copied under the actual owning allocator lock. Extent is the actual
// native allocation request span owned/freed by that owner, never CRT overhead.
internal sealed record AllocatorBlockWindow434(
    string Allocator,
    string Type,
    Term Identity,
    ulong Bytes
);
