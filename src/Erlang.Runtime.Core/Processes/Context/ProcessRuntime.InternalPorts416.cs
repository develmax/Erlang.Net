namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireInternalPortActor416(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
    }
}
