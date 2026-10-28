namespace Erlang;

internal static class RuntimeStoredFunctionRecipes
{
    internal static IRuntimeStoredFunctionRecipe Capture(ProcessRuntime owner, FunctionTerm function)
        => owner.Functions.TryCaptureStored(function, out IRuntimeStoredFunctionRecipe? recipe)
            ? recipe!
            : new BeamStoredFunctionRecipe(BeamClosureFactory.CaptureStoredBinding(owner, function));

    internal static bool TryCaptureTransfer506(ProcessRuntime owner, FunctionTerm function, out IRuntimeStoredFunctionRecipe? recipe)
    {
        if (owner.Functions.TryCaptureStored(function, out recipe))
            return true;
        if (BeamClosureFactory.TryCaptureTransferBinding506(owner, function, out BeamClosureBinding? binding))
        {
            recipe = new BeamStoredFunctionRecipe(binding!);

            return true;
        }
        recipe = null;

        return false;
    }
}
