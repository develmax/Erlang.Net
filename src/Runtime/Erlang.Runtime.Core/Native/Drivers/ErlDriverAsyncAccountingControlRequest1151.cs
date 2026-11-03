namespace Erlang;

internal sealed record ErlDriverAsyncAccountingControlRequest1151(
    int Version,
    string Profile,
    ulong Epoch,
    string Generation,
    ulong Token,
    string Action
);
