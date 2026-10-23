using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed class NifNativeHostFailureException : Exception
{
    internal ExceptionDispatchInfo HostFailure { get; }
    internal Term GuestPendingReason { get; }

    internal NifNativeHostFailureException(ExceptionDispatchInfo hostFailure, Term guestPendingReason)
        : base(NifNativeDiagnostics.HostAndGuestFailure, hostFailure.SourceException)
    {
        HostFailure = hostFailure;
        GuestPendingReason = guestPendingReason;
    }
}
