namespace Erlang;

public sealed partial class ModuleRegistry
{
    private ValueTask<Term> InvokeAdmittedSelection(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        ErlangFunction body,
        ModuleCodeImage? image,
        IDisposable? code,
        bool handler
    )
    {
        ValueTask<Term> InvokeOwned()
        {
            try
            {
                context.Runtime.ThrowIfHalted();
                context.Cancellation.ThrowIfCancellationRequested();
            }
            catch
            {
                code?.Dispose();
                if (code is not null)
                    context.RestoreSourceCodeFlow(code);
                throw;
            }

            return handler
                ? InvokeErrorHandler(
                    context,
                    module,
                    function,
                    arguments,
                    body,
                    image,
                    code
                )
                : InvokeSelected(
                    context,
                    module,
                    function,
                    arguments,
                    body,
                    image,
                    code
                );
        }

        var scheduler = context.ActorTaskScheduler;
        if (scheduler is null || RuntimeActorExecutionTurn.IsOwnedBy(context))
            return InvokeOwned();
        try
        {
            // Task creation captures the selected source lease/flow before the
            // caller restores it. The admitted invocation owns eventual disposal.
            return new ValueTask<Term>(scheduler.Invoke(InvokeOwned));
        }
        catch
        {
            code?.Dispose();
            throw;
        }
        finally
        {
            if (code is not null)
                context.RestoreSourceCodeFlow(code);
        }
    }
}
