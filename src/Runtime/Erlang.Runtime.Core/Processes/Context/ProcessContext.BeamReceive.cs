namespace Erlang;

public sealed partial class ProcessContext
{
    internal ValueTask<T?> ReceiveBeamAdmitted593<T>(Func<Term, ValueTask<T?>> select, Term timeout, MailboxSelectionAdmission593? admission593 = null) where T : class =>
        ReceiveBeamSelectionAdmitted593(
            select,
            timeout,
            rescanRemovedCandidate: false,
            admission593
        );

    internal ValueTask<T?> ReceiveBeamGuardAdmitted593<T>(Func<Term, ValueTask<T?>> select, Term timeout, MailboxSelectionAdmission593? admission593 = null) where T : class =>
        ReceiveBeamSelectionAdmitted593(
            select,
            timeout,
            rescanRemovedCandidate: true,
            admission593
        );

    private async ValueTask<T?> ReceiveBeamSelectionAdmitted593<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout,
        bool rescanRemovedCandidate,
        MailboxSelectionAdmission593? admission593
    ) where T : class
    {
        Runtime.ThrowIfHalted();
        try
        {
            T? result = await (rescanRemovedCandidate
                ? Mailbox.ReceiveBeamGuardAdmitted593(
                    select,
                    timeout,
                    Cancellation,
                    admission593
                )
                : Mailbox.ReceiveBeamAdmitted593(
                    select,
                    timeout,
                    Cancellation,
                    admission593
                ));
            try
            {
                Runtime.ThrowIfHalted();
                Cancellation.ThrowIfCancellationRequested();

                return result;
            }
            catch
            {
                if (result is BeamReceiveSelection abandoned507)
                    abandoned507.Dispose();
                throw;
            }
        }
        catch (OperationCanceledException) when (Runtime.IsHaltRequested)
        {
            throw new ProcessHaltTransfer(Runtime);
        }
    }
}
