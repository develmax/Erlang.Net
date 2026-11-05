namespace Erlang;

public sealed partial class ModuleRegistry
{
    private bool managedErtsInternalLibrary;
    internal bool HasManagedErtsInternalLibrary => managedErtsInternalLibrary;

    internal FunctionTerm CreateErtsInternalResumeAction(ProcessContext context, Term reference)
    {
        PreparedBeamImage image = statisticsTrapPrivateImage ?? throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        var entry = image.Code.Lambdas.Select((lambda, index) => (lambda, index)).Single(entry =>
            image.Code.Structure.Atoms[entry.lambda.FunctionAtomIndex] == ErtsInternalResumeRecipe.Function);
        var identity = new LocalFunctionIdentity(
            Term.A(ErtsInternalContract.Module),
            Term.A(ErtsInternalResumeRecipe.Function),
            entry.lambda.StoredIndex,
            entry.lambda.OldUniqueness,
            image.Descriptor.Fingerprint,
            entry.index
        );

        return new ErtsInternalResumeRecipe(context.Runtime.Functions, identity).Rebind([context.Self, reference]);
    }

    internal void InstallErtsInternalLibrary(bool managed)
    {
        // The authenticated image supplies only the stub inventory and the same
        // captured C# primitive delegates used by the original BEAM backend.
        // No Erlang library body is executed by the managed implementation.
        PreparedBeamImage image;
        lock (statisticsTrapGate)
        {
            image = statisticsTrapPrivateImage ?? throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        }
        InstallPreloaded(registry =>
        {
            foreach (var entry in image.ExecutionIdentity.NativePrimitives)
            {
                if (registry.HasExport(ErtsInternalContract.Module, entry.Key.Function, entry.Key.Arity))
                {
                    continue;
                }
                registry.Register(
                    ErtsInternalContract.Module,
                    entry.Key.Function,
                    entry.Key.Arity,
                    entry.Value
                );
            }
        });
        if (managed)
        {
            foreach (var export in ErtsInternalContract.LibraryExports)
            {
                var key = (ErtsInternalContract.Module, export.Function, export.Arity);
                if (!functions.ContainsKey(key))
                {
                    continue;
                }
                // Only the two bootstrap compiler intrinsics are replaceable.
                // A caller-installed library must never be silently displaced.
                if (!MapNativeBridge.IsCompilerHelper(key.Module, key.Function, key.Arity)
                    || !IsRuntimeIntrinsic(key.Module, key.Function, key.Arity))
                {
                    throw StartupOwnerFailure(ErtsInternalContract.Module);
                }
                functions.Remove(key);
                intrinsicBindings.Remove(key);
                runtimeErrorBindings.Remove(key);
                ownedBindings507.Remove(key);
            }
            ErtsInternalLibrary.Register(this);
            managedErtsInternalLibrary = true;
        }
    }
}
