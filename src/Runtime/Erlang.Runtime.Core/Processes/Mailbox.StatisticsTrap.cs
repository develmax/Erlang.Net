namespace Erlang;

public sealed partial class Mailbox
{
    private readonly AsyncLocal<StatisticsTrapReceiveFailure?> statisticsTrapFailure = new();

    internal IDisposable EnterStatisticsTrapReceive(StatisticsTrapReceiveFailure failure)
    {
        StatisticsTrapReceiveFailure? previous = statisticsTrapFailure.Value;
        statisticsTrapFailure.Value = failure;

        return new StatisticsTrapReceiveScope(statisticsTrapFailure, previous);
    }

    private void ThrowStatisticsTrapFailure() => statisticsTrapFailure.Value?.ThrowIfFailed();

    private async Task WaitStatisticsTrapSignal(Task signal, TimeSpan? timeout, CancellationToken cancellation)
    {
        StatisticsTrapReceiveFailure? failure = statisticsTrapFailure.Value;
        if (failure is null)
        {
            if (timeout is { } finite)
                await signal.WaitAsync(finite, cancellation);
            else
                await signal.WaitAsync(cancellation);

            return;
        }
        ThrowIfHalted();
        cancellation.ThrowIfCancellationRequested();
        failure.ThrowIfFailed();
        Task ready = Task.WhenAny(signal, failure.Failure);
        if (timeout is { } limit)
            await ready.WaitAsync(limit, cancellation);
        else
            await ready.WaitAsync(cancellation);
        ThrowIfHalted();
        cancellation.ThrowIfCancellationRequested();
        failure.ThrowIfFailed();
        await signal;
    }

    // Request callbacks have ALL physically settled before this protocol-only cleanup.
    internal void RemoveStatisticsTrapReplies(ReferenceTerm reference)
    {
        List<Exception>? failures = null;
        lock (gate)
        {
            // An interrupted genuine receive must not leave its save cursor past
            // unrelated messages when the caller continues after the BIF error.
            ResetBeamSave();
            for (var node = messages.First; node is not null;)
            {
                var next = node.Next;
                Term value = node.Value.Value;
                bool matches = value is ReferenceTerm bare && bare.Equals(reference)
                    || value is TupleTerm { Items.Count: 2 } pair
                        && pair.Items[0] is ReferenceTerm first && first.Equals(reference);
                if (!node.Value.Control && node.Value.Admission is null && matches)
                    try
                    {
                        RemoveNode(node);
                    }
                    catch (Exception error)
                    {
                        (failures ??= new()).Add(error);
                    }
                if (Volatile.Read(ref closed593))
                    break;
                node = next;
            }
        }
        if (failures is not null)
            throw new AggregateException(failures);
    }
}
