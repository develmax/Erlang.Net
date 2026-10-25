namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireInternalActor419(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
    }

    internal ProcessTableBatch? ReadNativeProcessBatch419(ProcessContext context, int cursor)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();

            return processSlots.ReadNative419(cursor, context.RemainingSlice419);
        }
    }
}
