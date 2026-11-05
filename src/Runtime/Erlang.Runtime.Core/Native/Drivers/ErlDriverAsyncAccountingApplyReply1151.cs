namespace Erlang;

internal sealed record ErlDriverAsyncAccountingApplyReply1151(
    int Version,
    string Profile,
    ulong Epoch,
    string Generation,
    ulong Token,
    string Action,
    ErlDriverAsyncAccountingRow1151[] Rows
);
