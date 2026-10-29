namespace Erlang;

public sealed partial class ProcessContext
{
    internal ValueTask<Term> InvokeGuestFunction507(FunctionTerm function, IReadOnlyList<Term> arguments)
        => !OwnedExecutionEnabled683 || !RuntimeActorExecutionTurn.IsOwnedBy(this)
            || CurrentOwned506 is not { IsDisposed: false }
            ? function.Invoke(this, arguments) : AwaitGuestFunction507(function, arguments);

    private async ValueTask<Term> AwaitGuestFunction507(FunctionTerm function, IReadOnlyList<Term> arguments)
    {
        bool external = Runtime.Functions.IsExternalEntry(function);
        bool canonical = RuntimeStoredFunctionRecipes.TryCaptureTransfer506(Runtime, function, out var recipe)
            && recipe is ICanonicalGuestRecipe507 admitted && admitted.AdmittedTo507(Runtime);
        if (!external && !canonical)
            return await function.Invoke(this, arguments);
        using var owned = EnterOwnedInvocation506(arguments)!;
        owned.MarkCompilerFactory507();
        owned.HoldGuestResult507(function); // Root callable + environment before any await.
        try
        {
            Term result = external
                ? await Runtime.Modules.CallInto506(
                    this,
                    function.ExternalIdentity!.Module.Name,
                    function.ExternalIdentity.Function.Name,
                    owned.Arguments
                )
                : await function.InvokeDirect506(this, owned.Arguments);

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
