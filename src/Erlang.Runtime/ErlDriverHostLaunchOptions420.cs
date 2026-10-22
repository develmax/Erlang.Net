namespace Erlang;

internal sealed record ErlDriverHostLaunchOptions420(
    TimeSpan StartupTimeout,
    TimeSpan ShutdownTimeout,
    int MaximumFrameBytes
);
