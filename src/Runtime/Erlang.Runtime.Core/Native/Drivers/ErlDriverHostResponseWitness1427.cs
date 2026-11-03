namespace Erlang;

// Read-only evidence of a genuine correlated envelope. Typed payload and event
// prefix acceptance still belong to the unchanged subsequent production flow.
internal sealed record ErlDriverHostResponseWitness1427(
    ErlDriverHostOperation420 Operation,
    ulong Request,
    ulong Driver,
    ulong Port,
    ulong Actor,
    uint Command,
    ulong EventSequence,
    string? PayloadSha256,
    Task PendingCompletion
);
