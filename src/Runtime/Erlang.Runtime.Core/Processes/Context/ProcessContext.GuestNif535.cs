namespace Erlang;

public sealed partial class ProcessContext
{
    internal ValueTask<Term> InvokeImageNif535(NifImageCall native, IReadOnlyList<Term> arguments)
        => native.GuestNative535 && Runtime.OwnedTermsPilot506
            && CurrentOwned506 is { IsDisposed: false } && RuntimeActorExecutionTurn.IsOwnedBy(this)
                ? AwaitGuestNif535(native, arguments)
                : InvokeNativePrimitive507(native.InvokeAsync, arguments);

    private async ValueTask<Term> AwaitGuestNif535(NifImageCall native, IReadOnlyList<Term> arguments)
    {
        using var owned = EnterOwnedInvocation506(arguments)!;
        owned.BindNativeGuestResult535(native);
        try
        {
            Term result = await native.InvokeAsync(this, owned.Arguments);

            return owned.CompleteGuest507(result);
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
