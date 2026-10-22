namespace Erlang;

public sealed record RuntimeAtomicsReport413(
    long Created,
    long Freed,
    long LiveArrays,
    ulong OwnedNativeBytes,
    int ActiveInvocations,
    bool Closed
);
