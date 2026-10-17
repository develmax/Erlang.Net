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

    internal ValueTask<Term> InvokeSourceLoadNif(
        ProcessContext context,
        Term[] arguments
    )
    {
        var handler = Volatile.Read(ref sourceLoadNifHandler);
        if (handler is null && Volatile.Read(ref nativeExecutionPolicy) is { } approval)
            handler = (caller, captured, values) => NifNativeDefaultLoader.LoadAsync(
                caller,
                captured,
                values,
                approval
            );
        if (handler is null)
        {
            if (!SourceLoadNifFilename.IsValidUtf8(arguments[0]))
                return RuntimeExceptionCapture.InvokeNative(
                    context,
                    (_, _) => throw new ErlangException(ErlangErrorReasons.BadArgument),
                    ErlangModuleNames.Module,
                    SourceLoadNifNames.Function,
                    arguments
                );

            return Call(
                context,
                ErlangModuleNames.Module,
                SourceLoadNifNames.Function,
                arguments
            );
        }

        return RuntimeExceptionCapture.InvokeNative(
            context,
            (caller, _) => InvokeSourceLoadNifCore(caller, arguments, handler),
            ErlangModuleNames.Module,
            SourceLoadNifNames.Function,
            arguments
        );
    }

    private async ValueTask<Term> InvokeSourceLoadNifCore(
        ProcessContext context,
        Term[] arguments,
        Func<ProcessContext, SourceLoadNifCall, Term[], ValueTask<Term>> handler
    )
    {
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
