namespace Erlang;

internal sealed partial class PreparedMapGraph517
{
    private readonly Dictionary<FunctionTerm, PreparedCanonicalFunction> canonicalFunctions = new(ReferenceEqualityComparer.Instance);

    internal PreparedCanonicalFunction? FindCanonicalFunction(FunctionTerm function) => canonicalFunctions.GetValueOrDefault(function);

    private void CaptureCanonicalFunction(ProcessRuntime runtime, Term value)
    {
        if (value is not FunctionTerm { ExternalIdentity: null, StoredLocalIdentity: not null, OwnedFunctionStorage: null } function
            || runtime.IsSharingLiteral425(function) || !RuntimeStoredFunctionRecipes.TryCaptureTransfer506(runtime, function, out var recipe)
            || recipe is not ICanonicalGuestRecipe507 admitted || !admitted.AdmittedTo507(runtime)) return;
        int arity = function.Arity;
        Term[] environment = function.Environment.ToArray();
        if (arity > byte.MaxValue || environment.Length > byte.MaxValue) return;
        // Authentication above is source/image-specific. Factory rebinding must
        // finish OUTSIDE H; never attach guest storage to a retained Host wrapper.
        FunctionTerm rebound = recipe.Rebind(environment);
        if (ReferenceEquals(rebound, function) || rebound.OwnedFunctionStorage is not null
            || rebound.Arity != arity || rebound.Environment.Count != environment.Length)
            throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        for (int index = 0; index < environment.Length; index++)
            if (!ReferenceEquals(rebound.Environment[index], environment[index]))
                throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        canonicalFunctions.Add(
            function,
            new(rebound, arity, environment)
            { StableGlobals = PreparedCanonicalFunction.CaptureStableGlobals(runtime, environment) }
        );
    }
}
