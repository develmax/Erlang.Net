namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool ChangeAsyncDistribution(ProcessContext context, bool value)
    {
        lock (gate)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (!ReferenceEquals(context.Runtime, this) || !processes.TryGetValue(context.Self, out var owned) ||
                !ReferenceEquals(context, owned))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            bool previous = context.AsyncDistribution;
            context.AsyncDistribution = value;

            return previous;
        }
    }
}
