namespace Erlang;

public sealed partial class ModuleRegistry
{
    private ValueTask<Term> InvokeErrorHandler(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        ErlangFunction body,
        ModuleCodeImage? image,
        IDisposable? code
    )
    {
        try
        {
            ValueTask<Term> pending = InvokeSelected(
                context,
                module,
                function,
                arguments,
                body,
                image,
                code
            );

            return pending.IsCompletedSuccessfully ? pending : AwaitErrorHandler(
                pending,
                module,
                function,
                arguments
            );
        }
        catch (ErlangException exception)
        {
            throw exception.WithSelectedHandlerFrame(module, function, arguments);
        }
    }

    private static async ValueTask<Term> AwaitErrorHandler(
        ValueTask<Term> pending,
        string module,
        string function,
        Term[] arguments
    )
    {
        try
        {
            return await pending;
        }
        catch (ErlangException exception)
        {
            throw exception.WithSelectedHandlerFrame(module, function, arguments);
        }
    }
}
