namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void InitializationEndedWithSequence(ProcessContext context)
    {
        try
        {
            TerminateWithSequencePreparation(context, context.ExitReason ?? Term.A(ProcessExitReasons.Shutdown));
            CompleteInitializationLogicalShutdown();
        }
        finally
        {
            try
            {
                try
                {
                    ProcessTracing.RetirePatternRoots778();
                }
                finally
                {
                    RetireSequenceShutdownAsync().GetAwaiter().GetResult();
                }
            }
            finally
            {
                try
                {
                    context.BeginOwnedTermRetirement506();
                }
                finally
                {
                    context.Unwound.TrySetResult();
                    if (!nifShutdownOwnedByPreparedSequence) StartNifShutdown();
                }
            }
        }
    }

    private void CompleteInitializationLogicalShutdown()
    {
        while (true)
        {
            lock (gate)
                if (sequenceLogicalShutdownCommitted || disposed || IsHaltRequested) return;
            PreparedSequenceShutdownBatch batch = PrepareSequenceShutdownAsync().GetAwaiter().GetResult();
            try
            {
                lock (gate)
                {
                    if (sequenceLogicalShutdownCommitted || disposed || IsHaltRequested) return;
                    if (!AcceptSequenceShutdown(batch)) continue;
                    CommitSequenceLogicalShutdown();

                    return;
                }
            }
            finally
            {
                batch.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }
}
