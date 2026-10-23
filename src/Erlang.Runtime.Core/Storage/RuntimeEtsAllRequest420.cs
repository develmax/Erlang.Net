namespace Erlang;

internal sealed class RuntimeEtsAllRequest420
{
    private readonly RuntimeEtsFoundation foundation;
    internal ProcessContext Context { get; }
    internal ReferenceTerm Reference { get; }
    internal RuntimeSchedulerAuxiliaryBatch420 Batch { get; }
    internal Task Completion { get; }
    internal bool Registered;

    internal RuntimeEtsAllRequest420(RuntimeEtsFoundation foundation, ProcessContext context, ReferenceTerm reference)
    {
        this.foundation = foundation;
        Context = context;
        Reference = reference;
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        if (suppress) flow = ExecutionContext.SuppressFlow();
        try
        {
            Batch = context.Runtime.PrepareEtsAllLanes420(RunLane);
            // Registered before any work can complete; no task/delegate allocation after publication.
            Completion = Batch.Completion.ContinueWith(
                static (completed, state) =>
             ((RuntimeEtsAllRequest420)state!).Finish(completed),
                this,
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

    private void RunLane(int identity) => foundation.ReplyAllLane420(this, identity);

    private void Finish(Task completed) => foundation.AllRequestEnded420(this, completed.Exception?.InnerExceptions);
}
