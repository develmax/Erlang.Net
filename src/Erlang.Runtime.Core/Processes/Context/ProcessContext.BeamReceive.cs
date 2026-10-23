namespace Erlang;

public sealed partial class ProcessContext
{
    internal async ValueTask<T?> ReceiveBeamAsync<T>(Func<Term, ValueTask<T?>> select, Term timeout) where T : class
    {
        Runtime.ThrowIfHalted();
        try
        {
            T? result = await Mailbox.ReceiveBeamAsync(select, timeout, Cancellation);
            Runtime.ThrowIfHalted();

            return result;
        }
        catch (OperationCanceledException) when (Runtime.IsHaltRequested)
        {
            throw new ProcessHaltTransfer(Runtime);
        }
    }
}
