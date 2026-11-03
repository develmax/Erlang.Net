namespace Erlang;

internal sealed record ErlDriverAsyncAccountingAck1151(
    int Version,
    string Profile,
    ulong Epoch,
    string Generation,
    ulong Token,
    string Phase
);
