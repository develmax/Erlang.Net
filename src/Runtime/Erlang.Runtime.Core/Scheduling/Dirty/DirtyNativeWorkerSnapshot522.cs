namespace Erlang;

internal sealed record DirtyNativeWorkerSnapshot522(
    DirtyNativeKind419 Kind,
    int ThreadId,
    bool Alive,
    bool Quiesced,
    bool Joined
);
