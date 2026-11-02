namespace Erlang;

// A read snapshot authenticated by the actual source heap while its queue
// operation is held. This DTO does not own a native reference or pin bytes.
internal sealed record ReadonlyTraceBinary817(
    BitString Source,
    OrdinaryBinaryBacking Owner,
    BinaryBacking Backing,
    ResourceBitsRead577 Bits
);
