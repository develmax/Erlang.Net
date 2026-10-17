namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal bool ValidateLocalFunctionPublication(LocalFunctionInvocation invocation)
    {
        lock (gate)
            return invocation.IsCurrent;
    }

    internal ValueTask<Term> InvokeUndefinedLambda(
        ProcessContext context,
        string module,
        FunctionTerm original,
        IReadOnlyList<Term> arguments,
        LocalFunctionInvocation? invocation,
        bool incompatibleFactory = false
    )
    {
        context.Runtime.ThrowIfHalted();
        foreach (var argument in arguments)
            context.Runtime.ObserveIncomingTerm(argument);
        Atom handler = context.ErrorHandler;
        ErlangFunction? body = null;
        ModuleCodeImage? image;
        IDisposable? code;
        Term[] callbackArguments;
        bool hostErrorHandler;
        lock (gate)
        {
            bool badfun = incompatibleFactory || loadedModules.ContainsKey(module);
            bool callback = !badfun && functions.TryGetValue((handler.Name, ProcessErrorHandlerNames.UndefinedLambda, ProcessErrorHandlerNames.Arity), out body);
            image = callback ? SelectErrorHandlerImage(handler.Name, ProcessErrorHandlerNames.UndefinedLambda, body!) : null;
            callbackArguments = callback ? [Term.A(module), original, Cons.From(arguments)] : [];
            if (invocation is not null && !invocation.IsCurrent)
                throw new LocalFunctionPublicationChangedException(invocation);
            if (badfun)
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), original));
            if (!callback)
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
            code = image is null ? null : context.EnterSourceCode(image, body!);
            hostErrorHandler = !sourceOwners.ContainsKey((handler.Name, ProcessErrorHandlerNames.UndefinedLambda, ProcessErrorHandlerNames.Arity));
        }

        return InvokeErrorHandler(
            context,
            handler.Name,
            ProcessErrorHandlerNames.UndefinedLambda,
            callbackArguments,
            body!,
            image,
            code,
            hostErrorHandler
        );
    }
}
