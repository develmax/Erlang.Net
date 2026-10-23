namespace Erlang;

internal sealed record ErlDriverHostPending420(
    ErlDriverHostOperation420 Operation,
    ulong Driver,
    ulong Port,
    ulong Actor,
    uint Command,
    bool SchedulerThread,
    TaskCompletionSource<ErlDriverHostFrame420> Completion
)
{
    internal ErlDriverHostFrame420? Reply;
}
