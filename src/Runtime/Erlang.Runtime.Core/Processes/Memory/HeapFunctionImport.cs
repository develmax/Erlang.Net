namespace Erlang;

internal static class HeapFunctionImport
{
    // The caller keeps the complete source producer and destination staging
    // roots live. This private recipe/opaque construction is outside H/D/R/M.
    internal static HeapFunctionRebinding? Rebind(ProcessRuntime runtime, FunctionTerm source, Term[] environment)
    {
        if (RuntimeStoredFunctionRecipes.TryCaptureTransfer506(runtime, source, out var recipe))
        {
            // Existing exact canonical external pointers remain VM cache values;
            // a physical external-alias producer is a separate reviewed gate.
            if (recipe is ExternalStoredFunctionRecipe) return null;
            FunctionTerm result = recipe!.Rebind(environment);
            Validate(result, environment);

            return new(
                source,
                result,
                environment,
                PreparedCanonicalFunction.CaptureStableGlobals(runtime, environment)
            );
        }
        if (source.HasRuntimeLocalTransferDescriptor)
            throw new NotSupportedException(ProcessTerm506Names.UnknownClosure);
        FunctionTerm opaque = source.CopyOpaqueForHeapStorage(environment);
        Validate(opaque, environment);

        return new(
            source,
            opaque,
            environment,
            PreparedCanonicalFunction.CaptureStableGlobals(runtime, environment)
        );
    }

    private static void Validate(FunctionTerm result, Term[] environment)
    {
        if (result.Environment.Count != environment.Length)
            throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        for (int index = 0; index < environment.Length; index++)
            if (!ReferenceEquals(result.Environment[index], environment[index]))
                throw new InvalidOperationException(HeapFunctionLayout.Invalid);
    }
}
