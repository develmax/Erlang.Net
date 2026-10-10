using System.Diagnostics;

namespace Erlang.Otp;

public sealed record ChildSpec(
    Term Id,
    Func<ProcessContext, ValueTask<ProcessHandle>> Start,
    RestartPolicy Restart = RestartPolicy.Permanent,
    TimeSpan? Shutdown = null,
    Func<ProcessContext, ValueTask<ProcessHandle?>>? OptionalStart = null,
    Term? Type = null,
    Term? Modules = null,
    bool InfiniteShutdown = false,
    bool BrutalKill = false
);
