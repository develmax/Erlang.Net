namespace Erlang;

// Actual domain events/native bytes only; never a public ERTS word estimate.
internal sealed record NativeResourceDomainSnapshot524(
    long Created,
    long Reclaimed,
    long LiveNativeBytes,
    int LiveResources,
    int Pending,
    bool Closing,
    bool WorkerAlive,
    bool WorkerJoined,
    bool CompletedSuccessfully,
    Exception? Failure
);
