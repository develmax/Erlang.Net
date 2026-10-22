using Erlang;

namespace Erlang.Tests;

internal static class MailboxCompletion427Checks
{
    internal static async Task Run()
    {
        foreach (bool completedAtEntry in new[] { true, false })
        {
            var mailbox = new Mailbox();
            bool halted = completedAtEntry;
            var physical = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (completedAtEntry)
                physical.SetResult();
            typeof(Mailbox).GetProperty(MailboxCompletion427Names.Halt, MailboxCompletion427Names.Members)!
                .SetValue(mailbox, (Func<bool>)(() => halted));
            typeof(Mailbox).GetProperty(MailboxCompletion427Names.Signals, MailboxCompletion427Names.Members)!
                .SetValue(mailbox, (Action)(() =>
                {
                    halted = true;
                    physical.TrySetResult();
                }));

            await RequireCancellation(Wait(mailbox, physical.Task, CancellationToken.None));
            Require(halted && physical.Task.IsCompletedSuccessfully);
        }

        await Wait(new Mailbox(), Task.CompletedTask, CancellationToken.None);
        using var stopped = new CancellationTokenSource();
        stopped.Cancel();
        await RequireCancellation(Wait(new Mailbox(), Task.CompletedTask, stopped.Token));
    }

    private static ValueTask Wait(Mailbox mailbox, Task physical, CancellationToken cancellation)
    {
        return (ValueTask)typeof(Mailbox).GetMethod(MailboxCompletion427Names.Wait, MailboxCompletion427Names.Members)!
            .Invoke(mailbox, [physical, cancellation])!;
    }

    private static async Task RequireCancellation(ValueTask operation)
    {
        bool canceled = false;
        try
        {
            await operation;
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Require(canceled);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(MailboxCompletion427Names.Failure);
    }
}
