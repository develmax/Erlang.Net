namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Atom ChangeErrorHandler(ProcessContext context, Term value)
    {
        if (value is not Atom selected)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (!ReferenceEquals(context.Runtime, this) || !processes.TryGetValue(context.Self, out var owned) ||
                !ReferenceEquals(context, owned))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            Atom previous = context.ErrorHandler;
            context.ErrorHandler = selected;

            return previous;
        }
    }
}
