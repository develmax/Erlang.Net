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
        canonicalFunctions.Add(
            function,
            new(function, arity, environment)
            { StableGlobals = PreparedCanonicalFunction.CaptureStableGlobals(runtime, environment) }
        );
    }
}
