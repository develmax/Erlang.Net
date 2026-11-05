namespace Erlang;

internal sealed record ErlDriverAsyncAccountingCaptureReply1151(
    int Version,
    string Profile,
    ulong Epoch,
    string Generation,
    ulong Token,
    uint Count,
    ulong Frequency,
    ulong RawQpcFrequency
);
