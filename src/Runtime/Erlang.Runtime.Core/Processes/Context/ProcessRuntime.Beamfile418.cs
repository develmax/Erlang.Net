namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireBeamfileActor418(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
    }
}
