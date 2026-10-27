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
            // Opaque registered C# bodies see portable values, so a delegate that
            // retains its arguments cannot keep borrowed process-owned wrappers.
            Term[] portable = owned.Arguments.Select(owned.Export).ToArray();
            Term result = await CallWithDispatch(
                context,
                module,
                function,
                portable,
                out _
            );

            return owned.Complete(result);
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
