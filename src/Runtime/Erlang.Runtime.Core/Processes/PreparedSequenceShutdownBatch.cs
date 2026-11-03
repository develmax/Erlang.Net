namespace Erlang;

internal sealed class PreparedSequenceShutdownBatch : IAsyncDisposable
{
    internal ProcessContext[] Contexts { get; }
    internal PreparedSequenceTermination?[] Plans { get; }
    internal ProcessContext? Initial { get; set; }
    internal bool HasInitialProjection { get; set; }
    private int transferred, closed;
    internal bool IsTransferred => Volatile.Read(ref transferred) != 0;

    internal PreparedSequenceShutdownBatch(ProcessContext[] contexts)
    {
        Contexts = contexts;
        Plans = new PreparedSequenceTermination?[contexts.Length];
    }

    internal void TransferToRuntime()
    {
        if (Interlocked.Exchange(ref transferred, 1) != 0)
            throw new InvalidOperationException(SequenceTerminationNames.Unprepared);
    }

    internal PreparedSequenceTermination? For(ProcessContext context)
    {
        for (int index = 0; index < Contexts.Length; index++)
            if (ReferenceEquals(Contexts[index], context)) return Plans[index];
        throw new InvalidOperationException(SequenceTerminationNames.Unprepared);
    }

    public ValueTask DisposeAsync() => Volatile.Read(ref transferred) == 0 ? RetireAsync() : ValueTask.CompletedTask;

    internal async ValueTask RetireAsync()
    {
        if (Interlocked.Exchange(ref closed, 1) != 0) return;
        List<Exception>? failures = null;
        foreach (PreparedSequenceTermination? plan in Plans)
        {
            try
            {
                if (plan is not null && !plan.IsTransferred) await plan.RetireAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
