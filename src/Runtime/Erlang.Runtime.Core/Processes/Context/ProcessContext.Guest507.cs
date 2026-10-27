namespace Erlang;

public sealed partial class ProcessContext
{
    internal Term OwnEvaluationResult507(Term value)
    {
        var scope = CurrentOwned506;
        if (scope is { CanBorrow507: true } && scope.BindingKind507 is OwnedBindingKind507.CompilerGuest or OwnedBindingKind507.PreparedGuest)
            return scope.AdoptExpression507(value);

        return OwnGuestResult506(value);
    }

    internal Term CopyGuestEscape507(Term value)
        => Runtime.OwnedTermsPilot506 && CurrentOwned506 is { IsDisposed: false } scope ? scope.Export(value) : value;

    internal ValueTask<Term> InvokeNativePrimitive507(ErlangFunction primitive, IReadOnlyList<Term> arguments)
        => !Runtime.OwnedTermsPilot506 ? primitive(this, arguments) : AwaitNativePrimitive507(null, primitive, arguments);

    internal ValueTask<Term> InvokeNativePrimitive507(BeamExecutionIdentity execution, ErlangFunction primitive, IReadOnlyList<Term> arguments)
        => !Runtime.OwnedTermsPilot506 ? primitive(this, arguments) : AwaitNativePrimitive507(execution, primitive, arguments);

    private async ValueTask<Term> AwaitNativePrimitive507(BeamExecutionIdentity? execution, ErlangFunction primitive, IReadOnlyList<Term> arguments)
    {
        using var owned = EnterOwnedInvocation506(arguments)!;
        OwnedBindingKind507 kind = execution is null ? OwnedBindingKind507.Opaque : Runtime.Modules.NativeBorrowKind507(execution, primitive);
        bool borrowed = owned.CanBorrow507 && kind != OwnedBindingKind507.Opaque;
        owned.BindingKind507 = borrowed ? kind : OwnedBindingKind507.Opaque;
        try
        {
            Term[] selected = borrowed ? owned.Arguments : owned.Arguments.Select(owned.Export).ToArray();
            Term value = await primitive(this, selected);

            return borrowed ? owned.CompleteGuest507(value) : owned.Complete(value);
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
