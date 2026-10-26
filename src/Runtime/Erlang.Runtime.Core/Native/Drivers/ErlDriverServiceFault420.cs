namespace Erlang;

internal sealed class ErlDriverServiceFault420
{
    private readonly TaskCompletionSource<Exception> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Exception? failure;

    internal Exception? Failure => Volatile.Read(ref failure);
    internal Task<Exception> Completion => completion.Task;

    internal void Record(Exception error)
    {
        if (Interlocked.CompareExchange(ref failure, error, null) is not null) return;
        try
        {
            completion.TrySetResult(error);
        }
        catch { /* Failure remains in the pre-existing owner field. */ }
    }
}
