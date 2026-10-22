using System.Runtime.CompilerServices;

namespace Erlang;

public sealed partial class RuntimeFunctionEntries
{
    private readonly ConditionalWeakTable<FunctionTerm, IRuntimeStoredFunctionRecipe> storedSources = new();

    // Compiler is a reviewed friend assembly. No public SDK registration path.
    internal void RegisterStoredSource(FunctionTerm function, IRuntimeStoredFunctionRecipe recipe)
        => storedSources.Add(function, recipe);

    internal bool TryCaptureStored(FunctionTerm function, out IRuntimeStoredFunctionRecipe? recipe)
    {
        if (IsExternalEntry(function))
        {
            recipe = new ExternalStoredFunctionRecipe(this, function.ExternalIdentity!);

            return true;
        }

        return storedSources.TryGetValue(function, out recipe);
    }
}
