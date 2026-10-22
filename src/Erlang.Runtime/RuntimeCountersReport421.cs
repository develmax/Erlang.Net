namespace Erlang;

public sealed record RuntimeCountersReport421(
    long Created,
    long Freed,
    long LiveArrays,
    ulong OwnedNativeBytes,
    int ActiveInvocations,
    bool Closed
);
