namespace Erlang;

public sealed partial class ModuleRegistry
{
    private Func<ProcessContext, SourceLoadNifCall, Term[], ValueTask<Term>>? sourceLoadNifHandler;

    internal void InstallSourceLoadNifHandler(
        Func<ProcessContext, SourceLoadNifCall, Term[], ValueTask<Term>> handler
    )
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (gate)
        {
            if (sourceLoadNifHandler is not null)
                throw new InvalidOperationException(SourceLoadNifNames.HandlerAlreadyInstalled);
            Volatile.Write(ref sourceLoadNifHandler, handler);
        }
    }

    internal async ValueTask<Term> InvokeSourceLoadNif(
        ProcessContext context,
        Term[] arguments
    )
    {
        var handler = Volatile.Read(ref sourceLoadNifHandler);
        if (handler is null)
            return await Call(context, ErlangModuleNames.Module, SourceLoadNifNames.Function, arguments);
        if (!ReferenceEquals(context.Runtime.Modules, this))
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ForeignRuntime);
        context.Runtime.ThrowIfHalted();
        context.Cancellation.ThrowIfCancellationRequested();
        SourceCodeEntry caller = context.CaptureExecutingCode()
            ?? throw new NifImageAdmissionException(NifImageAdmissionFailure.NoExecutingCaller);
        using var captured = new SourceLoadNifCall(context, caller, ReserveSourceCode(context, caller.Image));
        foreach (Term argument in arguments)
            context.Runtime.ObserveIncomingTerm(argument);
        var invocation = context.Runtime.ProcessTracing.Called(
            context,
            ErlangModuleNames.Module,
            SourceLoadNifNames.Function,
            arguments
        );
        context.SavedCalls.Record(ErlangModuleNames.Module, SourceLoadNifNames.Function, arguments.Length);
        ValueTask<Term> pending;
        try
        {
            pending = handler(context, captured, arguments);
        }
        catch (ErlangException exception)
        {
            context.Runtime.ProcessTracing.Failed(context, invocation, exception);
            throw;
        }
        Term result = await context.Runtime.ProcessTracing.Complete(context, invocation, pending);
        context.Runtime.ThrowIfHalted();
        context.Runtime.ObserveIncomingTerm(result);

        return result;
    }
}
