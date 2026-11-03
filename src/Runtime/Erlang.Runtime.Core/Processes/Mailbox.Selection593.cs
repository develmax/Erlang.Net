namespace Erlang;

public sealed partial class Mailbox
{
    internal async ValueTask<T?> ReceiveGuestSelecting593<T>(
        Func<Term, ValueTask<T?>> select,
        Term? timeout,
        CancellationToken cancellation,
        MailboxSelectionAdmission593? admission
    ) where T : class
    {
        try
        {
            TimeSpan? initial = timeout is Integer integer && integer.Value.IsZero ? TimeSpan.Zero : null;

            return await ReceiveCore593(
                select,
                initial,
                cancellation,
                timeout is null ? null : () => ReceiveTimeoutValue.Parse(timeout),
                admission593: admission
            );
        }
        finally
        {
            WaitingStateChanged?.Invoke(false);
        }
    }
}
