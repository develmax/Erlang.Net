namespace Erlang;

internal static class RuntimeStoredFunctionRecipes
{
    internal static IRuntimeStoredFunctionRecipe Capture(ProcessRuntime owner, FunctionTerm function)
        => owner.Functions.TryCaptureStored(function, out IRuntimeStoredFunctionRecipe? recipe)
            ? recipe!
            : new BeamStoredFunctionRecipe(BeamClosureFactory.CaptureStoredBinding(owner, function));
}
