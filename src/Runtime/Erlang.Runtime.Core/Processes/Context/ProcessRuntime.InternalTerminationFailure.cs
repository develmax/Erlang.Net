namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Host bootstrap/retirement fault only. Public shutdown still passes the
    // original Source authority check; .NET host and other logical VMs survive.
    private void RequestInternalTerminationFailureHalt()
        => RequestHaltCore(
            new RuntimeHaltRequest(
                RuntimeHaltKind.Abort,
                SequenceTerminationNames.FatalStatus,
                SequenceTerminationNames.FatalPreparation,
                new RuntimeHaltOptions(false)
            ),
            internalFatalFailure: true
        );

    private void PreserveTerminalFailureAndStopOrphan(ProcessContext context, Exception primary)
    {
        bool orphan;
        lock (gate) orphan = processes.TryGetValue(context.Self, out ProcessContext? current) && ReferenceEquals(current, context)
            || sequenceLogicalShutdownCommitted && processes.Count != 0;
        if (!orphan || IsHaltRequested
            || primary is OperationCanceledException && context.Cancellation.IsCancellationRequested) return;
        context.Done.TrySetException(primary);
        try
        {
            RequestInternalTerminationFailureHalt();
        }
        catch (Exception cleanup)
        {
            throw new AggregateException(primary, cleanup);
        }
    }
}
