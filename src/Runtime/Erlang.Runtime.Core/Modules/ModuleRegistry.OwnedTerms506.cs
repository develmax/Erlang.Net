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

    private ValueTask<Term> CallOwnedBridge506(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        bool hostBoundary
    ) => context.Runtime.OwnedTermsPilot506
        ? AwaitOwnedCall506(
            context,
            module,
            function,
            arguments,
            hostBoundary
        )
        : CallWithDispatch(
            context,
            module,
            function,
            arguments,
            out _
        );

    private async ValueTask<Term> AwaitOwnedCall506(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        bool hostBoundary
    )
    {
        using OwnedTermInvocation506 owned = context.EnterOwnedInvocation506(arguments, hostBoundary)!;
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
                out _
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
