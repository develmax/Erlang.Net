namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Caller holds runtime gate. No mailbox lock/callback or wait is taken here.
    private static Task EtsQueueRetirement419(ProcessContext context, Task existing)
     => context.EtsQueueObservations419.Count == 0 ? existing : Task.WhenAll(context.EtsQueueObservations419.Append(existing));

    internal void ReleaseEtsQueueTracking419(ProcessContext context, LinkedListNode<Task> node, Task observation)
    {
        // Notify can be nested under a mailbox callback. Never acquire runtime gate there,
        // and retain the exact task until its outermost observation scope settles.
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        if (suppress) flow = ExecutionContext.SuppressFlow();
        try
        {
            _ = observation.ContinueWith(
                static (_, state) =>
            {
                var value = ((ProcessRuntime Runtime, ProcessContext Context, LinkedListNode<Task> Node))state!;
                value.Runtime.UntrackEtsQueue419(value.Context, value.Node);
            },
                (this, context, node),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default
            );
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    private void UntrackEtsQueue419(ProcessContext context, LinkedListNode<Task> node)
    {
        lock (gate)
            if (ReferenceEquals(node.List, context.EtsQueueObservations419)) context.EtsQueueObservations419.Remove(node);
    }
}
