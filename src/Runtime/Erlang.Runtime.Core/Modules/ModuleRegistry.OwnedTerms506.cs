namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal ValueTask<Term> CallInto506(
        ProcessContext context,
        string module,
        string function,
        params Term[] arguments
    )
        => CallOwnedBridge506(
            context,
            module,
            function,
            arguments,
            false
        );

    internal ValueTask<Term> CallWithNativeCallerScope(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        Func<ProcessContext, IDisposable>? nativeCaller
    ) => CallOwnedBridge506(
        context,
        module,
        function,
        arguments,
        true,
        nativeCaller
    );

    private ValueTask<Term> CallOwnedBridge506(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        bool hostBoundary,
        Func<ProcessContext, IDisposable>? nativeCaller = null
    ) => context.OwnedExecutionEnabled683
        ? AwaitOwnedCall506(
            context,
            module,
            function,
            arguments,
            hostBoundary,
            nativeCaller
        )
        : CallWithDispatch(
            context,
            module,
            function,
            arguments,
            out _,
            out _,
            nativeCaller
        );

    private async ValueTask<Term> AwaitOwnedCall506(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        bool hostBoundary,
        Func<ProcessContext, IDisposable>? nativeCaller = null
    )
    {
        using OwnedTermInvocation506 owned = hostBoundary
            ? OwnedTermInvocation506.CreatePublic414(context, arguments)
            : context.EnterOwnedInvocation506(arguments, false)!;
        try
        {
            // The selected binding bridge applies its exact body capability;
            // opaque callbacks export arguments before invoking host code.
            Term[] selectedArguments = owned.Arguments;
            Term result = await CallWithDispatch(
                context,
                module,
                function,
                selectedArguments,
                out _,
                out _,
                nativeCaller
            );

            return hostBoundary ? owned.Complete(result) : owned.CompleteGuest507(result);
        }
        catch (ErlangException error)
        {
            throw owned.Transfer(error);
        }
        catch (Exception control) when (control is IOwnedControlTransfer506)
        {
            throw owned.TransferControl506(control);
        }
    }
}
