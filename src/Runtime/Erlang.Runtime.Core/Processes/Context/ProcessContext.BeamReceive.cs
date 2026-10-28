namespace Erlang;

public sealed partial class ProcessContext
{
    internal ValueTask<T?> ReceiveBeamAsync<T>(Func<Term, ValueTask<T?>> select, Term timeout) where T : class =>
        ReceiveBeamSelectionAsync(select, timeout, rescanRemovedCandidate: false);

    internal ValueTask<T?> ReceiveBeamGuardAsync<T>(Func<Term, ValueTask<T?>> select, Term timeout) where T : class =>
        ReceiveBeamSelectionAsync(select, timeout, rescanRemovedCandidate: true);

    private async ValueTask<T?> ReceiveBeamSelectionAsync<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout,
        bool rescanRemovedCandidate
    ) where T : class
    {
        Runtime.ThrowIfHalted();
        try
        {
            T? result = await (rescanRemovedCandidate
                ? Mailbox.ReceiveBeamGuardAsync(select, timeout, Cancellation)
                : Mailbox.ReceiveBeamAsync(select, timeout, Cancellation));
            try
            {
                Runtime.ThrowIfHalted();

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
