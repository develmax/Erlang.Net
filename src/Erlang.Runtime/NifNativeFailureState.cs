using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed class NifNativeFailureState
{
    private Exception? hostFailure;

    internal Term? GuestPendingReason { get; private set; }
    internal bool HasGuestException => GuestPendingReason is not null;
    internal ExceptionDispatchInfo? HostFailure => hostFailure is null ? null : ExceptionDispatchInfo.Capture(hostFailure);

    internal void Raise(Term reason) => GuestPendingReason = reason;

    internal void FailHost(Exception exception)
        => hostFailure ??= exception;

    internal void ThrowPending()
    {
        if (hostFailure is not null && GuestPendingReason is { } guestReason)
            throw new NifNativeHostFailureException(ExceptionDispatchInfo.Capture(hostFailure), guestReason);
        if (hostFailure is not null)
            ExceptionDispatchInfo.Capture(hostFailure).Throw();
        if (GuestPendingReason is { } reason)
            throw new ErlangException(reason);
    }
}
