namespace Erlang;

public sealed partial class ProcessRuntime
{
    private PreparedSequenceShutdownBatch? pendingShutdownSequence;

    private async ValueTask<PreparedSequenceShutdownBatch> PrepareSequenceShutdownAsync(
        ProcessContext? initial = null,
        Term? initialReason = null,
        Pid? removedLink = null,
        ProcessLinkIdentity? removedOrigin = null
    )
    {
        AssertSequenceTracePreparation();
        while (true)
        {
            ProcessContext[] contexts;
            lock (gate) contexts = disposed || IsHaltRequested ? [] : processes.Values.ToArray();
            var batch = new PreparedSequenceShutdownBatch(contexts)
            { Initial = initial, HasInitialProjection = removedLink is not null || removedOrigin is not null };
            try
            {
                for (int index = 0; index < contexts.Length; index++)
                    batch.Plans[index] = await PrepareSingleSequenceTerminationAsync(
                        contexts[index],
                        ReferenceEquals(contexts[index], initial) ? initialReason! : Term.A(ProcessExitReasons.Shutdown),
                        ReferenceEquals(contexts[index], initial) ? removedLink : null,
                        ReferenceEquals(contexts[index], initial) ? removedOrigin : null
                    )
                        .ConfigureAwait(false);
                lock (gate)
                    if (IsCurrentSequenceShutdown(batch)) return batch;
            }
            catch (Exception primary)
            {
                try
                {
                    await batch.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(primary, cleanup);
                }
                if (primary is ObjectDisposedException)
                    lock (gate)
                        if (IsHaltRequested || disposed && processes.Count == 0)
                            return new PreparedSequenceShutdownBatch([]);
                throw;
            }
            await batch.DisposeAsync().ConfigureAwait(false);
        }
    }

    private bool IsCurrentSequenceShutdown(PreparedSequenceShutdownBatch batch, bool afterInitialAdmission = false)
    {
        if (disposed || IsHaltRequested) return batch.Contexts.Length == 0;
        if (!processes.Values.SequenceEqual(batch.Contexts, ReferenceEqualityComparer.Instance)) return false;
        for (int index = 0; index < batch.Contexts.Length; index++)
            if (batch.Plans[index] is { } plan
                ? !(batch.HasInitialProjection && !afterInitialAdmission && ReferenceEquals(batch.Contexts[index], batch.Initial)
                    ? plan.IsCurrentBeforeAdmission() : plan.IsCurrentBeforeTermination())
                : !ReferenceEquals(batch.Contexts[index], batch.Initial) && NeedsSingleSequenceTerminationPreparation(batch.Contexts[index])) return false;

        return true;
    }

    // Accepted before NIF/source/native admission begins closing. Context-null
    // roots and physical per-link packets already exist at this point.
    private bool AcceptSequenceShutdown(PreparedSequenceShutdownBatch batch, bool afterInitialAdmission = false)
    {
        if (!IsCurrentSequenceShutdown(batch, afterInitialAdmission)) return false;
        if (pendingShutdownSequence is not null) return false;
        batch.TransferToRuntime();
        pendingShutdownSequence = batch;

        return true;
    }

    private PreparedSequenceTermination? ShutdownPlanFor(ProcessContext context)
        => pendingShutdownSequence?.For(context);

    private async ValueTask RetireSequenceShutdownAsync()
    {
        PreparedSequenceShutdownBatch? batch;
        lock (gate)
        {
            batch = pendingShutdownSequence;
            pendingShutdownSequence = null;
        }
        if (batch is not null) await batch.RetireAsync().ConfigureAwait(false);
    }
}
