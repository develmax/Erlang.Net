namespace Erlang;

// Detached RAW counter words. Public conversion must reinterpret signed64.
internal sealed record ErlDriverAsyncAccountingRow1151(
    uint Identity,
    uint OperatingSystemThread,
    uint State,
    ulong Frequency,
    ulong PerformanceCounter,
    ulong[] Counters
);
