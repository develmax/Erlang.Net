namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal async ValueTask<SourceCodeLease> AcquireSourceEntry(
        ProcessContext context,
        ModuleCodeImage image,
        SourceCodeAdmissionKind kind,
        LocalFunctionInvocation? invocation = null
    )
    {
        while (true)
        {
            Task? pending;
            lock (gate)
            {
                if (invocation is not null && !invocation.IsCurrent)
                    throw new LocalFunctionPublicationChangedException(invocation);
                var state = purgeStates.GetOrCreateValue(image);
                if (state.Retired)
                    throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
                pending = state.Pending?.Task;
                if (pending is null || kind == SourceCodeAdmissionKind.Continuation && context.IsExecutingCode(image))
                    return context.ReserveSourceCode(image);
            }
            await context.Mailbox.WaitForControlCompletion(pending, context.Cancellation);
        }
    }
}
