namespace Erlang;

public sealed partial class ModuleRegistry
{
    private ModuleCodeImage? RequireStartupOwner(RuntimeModuleSelection choice)
    {
        string module = choice.Module;
        var entries = functions.Where(pair => pair.Key.Module == module).ToArray();
        bool hasImage = codeImages.TryGetValue(module, out ModuleCodeImage? image);
        bool preloaded = preloadedModules.ContainsKey(module);
        bool loaded = loadedModules.ContainsKey(module);
        bool managed = LibrarySelectionNames.ManagedModules.Contains(module, StringComparer.Ordinal);
        if (oldCode.ContainsKey(module) || sourceOwners.Keys.Any(key => key.Module == module))
            throw StartupOwnerFailure(module);
        if (!managed && !hasImage && !preloaded && !loaded && entries.Length == 0)
        {
            if (choice.Backend != RuntimeModuleBackend.Beam)
                throw StartupOwnerFailure(module);

            return null;
        }
        if (!hasImage || !preloaded || !loaded || entries.Length == 0)
            throw StartupOwnerFailure(module);
        foreach (var entry in entries)
        {
            var key = entry.Key;
            bool bound = runtimeErrorBindings.TryGetValue(key, out RuntimeErrorBinding? binding)
                && ReferenceEquals(binding.Body, entry.Value) && ReferenceEquals(binding.Image, image);
            if (!(bound || IsRuntimeIntrinsic(key.Module, key.Function, key.Arity))
                || !managed && !IsNativeEntry(key.Module, key.Function, key.Arity))
                throw StartupOwnerFailure(module);
        }
        if (!managed && choice.Backend != RuntimeModuleBackend.Beam)
            throw StartupOwnerFailure(module);

        return image;
    }

    private void RequireStartupNativeBootstrap(string module, PreparedBeamImage prepared)
    {
        foreach (var entry in functions.Where(pair => pair.Key.Module == module))
            if (!prepared.ExecutionIdentity.NativePrimitives.TryGetValue((entry.Key.Function, entry.Key.Arity), out ErlangFunction? body)
                || !ReferenceEquals(body, entry.Value))
                throw new InvalidDataException(LibrarySelectionNames.Primitive + LibrarySelectionNames.ContextSeparator + module
                    + LibrarySelectionNames.FunctionSeparator + entry.Key.Function + LibrarySelectionNames.AritySeparator + entry.Key.Arity);
    }

    private static InvalidOperationException StartupOwnerFailure(string module)
        => new(LibrarySelectionNames.Owner + LibrarySelectionNames.ContextSeparator + module);
}
