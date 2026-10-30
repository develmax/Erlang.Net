namespace Erlang;

public sealed partial class BeamModule
{
    private async ValueTask<Term> InvokePublicOwned(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        BeamLinkedFunction entry
    )
    {
        // Preserve validation before importing arbitrary public Host arguments.
        // This module owns immutable private prepared code, not a registry alias.
        BeamRegisterValidation.Validate(code, entry);
        context.AdmitOrdinaryGuestCollector();
        using var boundary = context.EnterOwnedInvocation506(arguments, true)!;
        using var guest = context.EnterOwnedInvocation506(boundary.Arguments)!;
        guest.BindingKind507 = OwnedBindingKind507.PreparedGuest;
        try
        {
            Term result = await BeamRegisterInterpreter.Execute(
                context,
                code,
                function,
                guest.Arguments,
                execution
            );

            return boundary.Complete(guest.CompleteGuest507(result));
        }
        catch (ErlangException error)
        {
            throw boundary.Transfer(guest.Transfer(error));
        }
        catch (Exception control) when (control is IOwnedControlTransfer506)
        {
            throw boundary.TransferControl506(guest.TransferControl506(control));
        }
    }
}
