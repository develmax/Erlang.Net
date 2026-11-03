namespace Erlang;

internal static class MailboxRootSettlement593
{
    // Invoked only by the queue root-release worker outside all monitors.
    // Execute is single-owner and drives the specific Ready work even when
    // its ThreadPool scheduling failed. If another worker claimed it, join
    // exactly that bounded typed-root disposal, never a global resource drain.
    internal static void Settle(MailboxPayloadLease593? last)
    {
        if (last is null)
            return;

        last.Execute();
        last.Completion.Task.GetAwaiter().GetResult();
    }

    // Sender transport finally must enter this outside runtime/mailbox gates.
    // Run Ready fallback before the first await, so progress does not require
    // another allocation/queue submission or global VM shutdown.
    internal static ValueTask SettleAsync(MailboxPayloadLease593? last)
    {
        if (last is null)
            return ValueTask.CompletedTask;

        last.Execute();

        return new ValueTask(last.Completion.Task);
    }
}
