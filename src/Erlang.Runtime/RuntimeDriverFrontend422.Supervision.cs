namespace Erlang;

internal sealed partial class RuntimeDriverFrontend422
{
    private void RecordFailure(Exception failure)
    {
        lock (gate)
        {
            if (!closing) fatal = failure;
            closing = true;
            ready = false;
            retirementRequested.TrySetResult();
        }
    }

    private void StartSupervision()
    {
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            supervision = Supervise();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    private async Task Supervise()
    {
        Task ended = await Task.WhenAny(
            child.Peer.Completion,
            drivers.Completion,
            monitors.Completion,
            retirementRequested.Task
        ).ConfigureAwait(false);
        if (ReferenceEquals(ended, retirementRequested.Task)) return;
        Exception failure;
        try
        {
            await ended.ConfigureAwait(false);
            failure = new IOException(DriverParentWireNames422.Closed);
        }
        catch (Exception actual)
        {
            failure = actual;
        }
        RecordFailure(failure);
    }
}
