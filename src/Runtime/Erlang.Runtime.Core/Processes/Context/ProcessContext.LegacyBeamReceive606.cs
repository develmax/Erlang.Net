namespace Erlang;

public sealed partial class ProcessContext
{
    internal ValueTask<T?> ReceiveBeamAsync<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout
    ) where T : class => ReceiveBeamAdmitted593(select, timeout, null);

    internal ValueTask<T?> ReceiveBeamGuardAsync<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout
    ) where T : class => ReceiveBeamGuardAdmitted593(select, timeout, null);

    private ValueTask<T?> ReceiveBeamSelectionAsync<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout,
        bool rescanRemovedCandidate
    ) where T : class => ReceiveBeamSelectionAdmitted593(
        select,
        timeout,
        rescanRemovedCandidate,
        null
    );
}
