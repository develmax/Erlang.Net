namespace Erlang;

internal sealed class SourceLoadNifCall(
    ProcessContext context,
    SourceCodeEntry caller,
    SourceCodeLease lease
) : IDisposable
{
    private readonly object gate = new();
    private SourceCodeLease? ownedLease = lease;
    private bool consumed;

    internal ModuleCodeImage Image => caller.Image;

    internal SourceCodeEntry Consume(ProcessContext executingContext)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context, executingContext) || ownedLease is null || consumed
                || !context.IsExecutingCode(caller.Image))
                throw new InvalidOperationException(SourceLoadNifNames.InvalidCapability);
            consumed = true;

            return caller;
        }
    }

    public void Dispose()
    {
        SourceCodeLease? released;
        lock (gate)
        {
            released = ownedLease;
            ownedLease = null;
        }
        released?.Dispose();
    }
}
