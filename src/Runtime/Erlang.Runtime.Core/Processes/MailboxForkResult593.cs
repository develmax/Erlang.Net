using System.Runtime.ExceptionServices;

namespace Erlang;

// Trusted runtime publication must retain this outcome until its outside-lock
// finally. Never discard a rejected token or inspect Failure and throw inside R.
internal readonly struct MailboxForkResult593(
    MailboxPayloadLease593? admitted,
    MailboxPayloadLease593? rejected,
    Exception? failure
)
{
    internal MailboxPayloadLease593? Admitted { get; } = admitted;
    internal MailboxPayloadLease593? Rejected { get; } = rejected;
    internal Exception? Failure { get; } = failure;

    internal async ValueTask<MailboxPayloadLease593?> ResolveOutsideMonitors()
    {
        Exception? cleanup = null;
        try
        {
            await MailboxRootSettlement593.SettleAsync(Rejected).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            cleanup = error;
        }

        if (Failure is not null && cleanup is not null)
            throw new AggregateException(Failure, cleanup);

        if (Failure is not null)
            ExceptionDispatchInfo.Capture(Failure).Throw();

        if (cleanup is not null)
            ExceptionDispatchInfo.Capture(cleanup).Throw();

        return Admitted;
    }
}
