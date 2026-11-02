namespace Erlang;

internal sealed partial class RuntimeProcessTaskScheduler
{
    // Called under the dispatcher gate immediately after Select + DequeueWork.
    // Epoch groups continuation priority; it is not a dispatch identity.
    internal void RecordSelectedProcessDispatch(RuntimeScheduledTask work)
    {
        if (Role != RuntimeActorRole.Guest
            || Volatile.Read(ref Owner.Terminated)
            || Owner.Cancellation.IsCancellationRequested
            || work.Task.IsCanceled
            || work.Task.IsCompleted)
            return;

        Owner.Runtime.Statistics.RecordSelectedProcessDispatch();
    }
}
