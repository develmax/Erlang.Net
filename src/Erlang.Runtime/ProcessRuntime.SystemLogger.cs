namespace Erlang;

public sealed partial class ProcessRuntime
{
    private Term systemLogger = RuntimeSystemLoggerAtoms.Logger;

    internal Term ReadSystemLogger()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ThrowIfHalted();

            return systemLogger;
        }
    }

    internal Term ExchangeSystemLogger(ProcessContext context, Term value)
    {
        if (!ReferenceEquals(context.Runtime, this))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        context.Cancellation.ThrowIfCancellationRequested();
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ThrowIfHalted();
            if (value is not Atom { Name: RuntimeSystemLoggerNames.Logger or RuntimeSystemLoggerNames.Undefined }
                && (value is not Pid pid || !IsCurrentLocal(pid)))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            // Native accepts an internal PID even when it is dead. Reset occurs
            // only on failed VM error-message delivery, never on process DOWN.
            Term previous = systemLogger;
            systemLogger = value;

            return previous;
        }
    }
}
