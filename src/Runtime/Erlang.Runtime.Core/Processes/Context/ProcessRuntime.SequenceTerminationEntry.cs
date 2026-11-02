namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void TerminateWithSequencePreparation(
        ProcessContext context,
        Term reason,
        RuntimeProcessPayload641? reason641 = null,
        Exception? sourceTeardown641 = null
    )
    {
        AssertSequenceTracePreparation();
        try
        {
            while (true)
            {
                PreparedSequenceTermination? prepared = PrepareSequenceTerminationAsync(context, reason).GetAwaiter().GetResult();
                Exception? attemptFailure = null;
                try
                {
                    lock (gate)
                    {
                        if (!processes.TryGetValue(context.Self, out ProcessContext? current) || !ReferenceEquals(current, context)) return;
                        if (prepared is not null ? !prepared.IsCurrentBeforeTermination() : NeedsSequenceTerminationPreparation(context)) continue;
                        Terminate(
                            context,
                            reason,
                            reason641,
                            sourceTeardown641,
                            prepared
                        );

                        return;
                    }
                }
                catch (Exception primary)
                {
                    attemptFailure = primary;
                    throw;
                }
                finally
                {
                    try
                    {
                        prepared?.DisposeAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception cleanup) when (attemptFailure is not null)
                    {
                        throw new AggregateException(attemptFailure, cleanup);
                    }
                }
            }
        }
        catch (Exception primary)
        {
            PreserveTerminalFailureAndStopOrphan(context, primary);
            throw;
        }
    }

    private async Task FinishPreparedSequenceContinuation(
        ProcessExitContinuation continuation,
        List<Exception> failures
    )
    {
        PreparedSequenceTermination previous = continuation.Sequence641
            ?? throw new InvalidOperationException(SequenceTerminationNames.Unprepared);
        while (true)
        {
            PreparedSequenceTermination refreshed = await previous.RefreshOutputsAsync().ConfigureAwait(false);
            bool accepted = false;
            try
            {
                lock (gate)
                {
                    if (!refreshed.AreOutputPlansCurrent()) continue;
                    refreshed.TransferToRuntime();
                    continuation.Sequence641 = refreshed;
                    accepted = true;
                    FinishTermination(
                        continuation.Process,
                        continuation.Reason,
                        failures,
                        continuation,
                        continuation.Origin641,
                        refreshed
                    );
                }

                return;
            }
            finally
            {
                if (!accepted) await refreshed.DisposeAsync().ConfigureAwait(false);
                else await previous.RetireAsync().ConfigureAwait(false);
            }
        }
    }
}
