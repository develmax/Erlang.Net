namespace Erlang;

public sealed partial class ModuleRegistry
{
    private async ValueTask<Term> InvokeAdmittedWithNativeCallerScope(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        ErlangFunction selected,
        ModuleCodeImage? image,
        IDisposable? code,
        bool handler,
        bool hostErrorHandler,
        OwnedBindingKind507 ownedKind,
        Func<ProcessContext, IDisposable> createScope
    )
    {
        IDisposable caller;
        try
        {
            // No user callback runs under the registry's selection gate.
            // Enter before tracing/admission so call/return-to sees the same PC.
            caller = createScope(context);
        }
        catch
        {
            // The admitted invocation has not yet taken this selected lease.
            try
            {
                code?.Dispose();
            }
            finally
            {
                if (code is not null)
                    context.RestoreSourceCodeFlow(code);
            }
            throw;
        }
        using (caller)
        {
            return await InvokeAdmittedSelection(
                context,
                module,
                function,
                arguments,
                selected,
                image,
                code,
                handler,
                hostErrorHandler,
                ownedKind
            );
        }
    }
}
