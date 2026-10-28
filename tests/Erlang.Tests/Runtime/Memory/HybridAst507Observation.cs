namespace Erlang.Tests;

internal sealed record HybridAst507Observation(
    bool CallbackExecuted,
    bool CallbackReceivedOwnedTuple,
    bool RetainedValueReadableAfterCollection,
    string? ReadFailureType
);
