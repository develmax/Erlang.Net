using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed class NifNativeFailureState
{
    private Exception? hostFailure;

    internal long PendingVersion { get; private set; }
    private Term? guestPendingReason;
    private Func<Term>? deferredGuestReason;
    internal Term? GuestPendingReason => deferredGuestReason?.Invoke() ?? guestPendingReason;
    internal bool HasGuestException => deferredGuestReason is not null || guestPendingReason is not null;
    internal ExceptionDispatchInfo? HostFailure => hostFailure is null ? null : ExceptionDispatchInfo.Capture(hostFailure);

    internal void Raise(Term reason)
    {
        guestPendingReason = reason;
        deferredGuestReason = null;
        PendingVersion++;
    }

    internal void RaiseDeferred(Func<Term> reason)
    {
        guestPendingReason = null;
        deferredGuestReason = reason;
        PendingVersion++;
    }

    internal void FailHost(Exception exception)
        => hostFailure ??= exception;

    internal void ThrowPending()
    {
        // Native callbacks may still write a new_binary after raising/querying an
        // exception. Freeze its reason at invocation completion, not at raise time.
        if (deferredGuestReason is { } deferred)
        {
            guestPendingReason = deferred();
            deferredGuestReason = null;
        }
        if (hostFailure is not null && guestPendingReason is { } guestReason)
            throw new NifNativeHostFailureException(ExceptionDispatchInfo.Capture(hostFailure), guestReason);
        if (hostFailure is not null)
            ExceptionDispatchInfo.Capture(hostFailure).Throw();
        if (guestPendingReason is { } reason)
            throw new ErlangException(reason);
    }
}
