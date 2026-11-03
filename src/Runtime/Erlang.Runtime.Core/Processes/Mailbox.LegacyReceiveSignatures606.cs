namespace Erlang;

public sealed partial class Mailbox
{
    // Preserve the original reflection-visible names and exact parameter counts.
    // The policy-aware implementation uses distinct names, never ambiguous overloads.
    internal ValueTask<T?> ReceiveBeamAsync<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout,
        CancellationToken cancellation
    ) where T : class => ReceiveBeamAdmitted593(
        select,
        timeout,
        cancellation,
        null
    );

    internal ValueTask<T?> ReceiveBeamGuardAsync<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout,
        CancellationToken cancellation
    ) where T : class => ReceiveBeamGuardAdmitted593(
        select,
        timeout,
        cancellation,
        null
    );

    private ValueTask<T?> ReceiveBeamSelectionAsync<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout,
        CancellationToken cancellation,
        bool rescanRemovedCandidate = false
    ) where T : class => ReceiveBeamSelectionAdmitted593(
        select,
        timeout,
        cancellation,
        rescanRemovedCandidate,
        null
    );

    private ValueTask<T?> ReceiveCore<T>(
        Func<Term, ValueTask<T?>> select,
        TimeSpan? timeout,
        CancellationToken cancellation,
        Func<TimeSpan?>? resolveTimeout = null,
        bool rescanRemovedCandidate = false
    ) where T : class => ReceiveCore593(
        select,
        timeout,
        cancellation,
        resolveTimeout,
        rescanRemovedCandidate,
        null
    );

    private ValueTask<T?> ReceiveMarkedCore<T>(
        Func<Term, ValueTask<T?>> select,
        TimeSpan? timeout,
        CancellationToken cancellation,
        Func<TimeSpan?>? resolveTimeout,
        ReceiveMarkerState? markerSelection
    ) where T : class => ReceiveMarkedCore593(
        select,
        timeout,
        cancellation,
        resolveTimeout,
        markerSelection,
        null
    );

    private ValueTask<T?> ReceiveBeamSaveCore<T>(
        Func<Term, ValueTask<T?>> select,
        TimeSpan? timeout,
        CancellationToken cancellation,
        Func<TimeSpan?>? resolveTimeout,
        bool rescanRemovedCandidate
    ) where T : class => ReceiveBeamSaveCore593(
        select,
        timeout,
        cancellation,
        resolveTimeout,
        rescanRemovedCandidate,
        null
    );
}
