namespace Erlang;

public sealed partial class ProcessContext
{
    public async ValueTask<T?> ReceiveSelectingAsync<T>(
        Func<Term, ValueTask<T?>> select,
        Term? erlangTimeout = null
    ) where T : class
    {
        Runtime.ThrowIfHalted();
        try
        {
            var result = await Mailbox.ReceiveSelectingAsync(
                select,
                null,
                Cancellation,
                erlangTimeout
            );
            Runtime.ThrowIfHalted();

            return result;
        }
        catch (OperationCanceledException) when (Runtime.IsHaltRequested)
        {
            throw new ProcessHaltTransfer(Runtime);
        }
    }
}
