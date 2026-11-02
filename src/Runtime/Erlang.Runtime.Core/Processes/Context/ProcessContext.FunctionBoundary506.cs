namespace Erlang;

public sealed partial class ProcessContext : ITermInvocationBoundary506
{
    bool ITermInvocationBoundary506.OwnedInvocationEnabled506 => OwnedExecutionEnabled683;

    async ValueTask<Term> ITermInvocationBoundary506.InvokeOwnedFunction506(FunctionTerm function, IReadOnlyList<Term> arguments)
    {
        // Public FunctionTerm.Invoke is always a new host boundary. Inherited
        // ExecutionContext data is never accepted as caller/root admission.
        using var owned = OwnedTermInvocation506.CreatePublic414(this, arguments);
        bool genuine = RuntimeActorExecutionTurn.IsOwnedBy(this)
            && (Runtime.Functions.IsExternalEntry(function)
                || RuntimeStoredFunctionRecipes.TryCaptureTransfer506(Runtime, function, out var recipe)
                    && recipe is ICanonicalGuestRecipe507 admitted && admitted.AdmittedTo507(Runtime));
        if (genuine)
            return await InvokePublicGuestFunction(function, owned);

        bool reconstruct = function.OwnedFunctionStorage is null
            && function.HasRuntimeLocalTransferDescriptor
            && !RuntimeStoredFunctionRecipes.TryCaptureTransfer506(Runtime, function, out _);
        FunctionTerm selected = (FunctionTerm)owned.Export(
            reconstruct ? owned.ImportPublicFunction414(function) : function
        );
        using var admission = EnterOwnedBeamFunAdmission650(function, selected);
        try
        {
            Term[] portable = owned.Arguments.Select(owned.Export).ToArray();
            Term result = await selected.InvokeDirect506(this, portable);

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
