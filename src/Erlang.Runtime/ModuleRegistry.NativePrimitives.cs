namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallNativePrimitives(ProcessRuntime runtime, RuntimeNativePrimitives selection)
    {
        RuntimeNativePrimitiveProviders.Validate(selection);
        if (!ReferenceEquals(owningRuntime, runtime))
            throw new ArgumentException(RuntimeNativePrimitiveNames.ForeignRegistry, nameof(runtime));
        if (selection == RuntimeNativePrimitives.None)
            return;
        using var source = EnterSourceMutation();
        Apply(target =>
        {
            if ((selection & RuntimeNativePrimitives.PersistentTermCore) != 0)
            {
                var bodies = new Dictionary<(string Function, int Arity), ErlangFunction>();
                foreach (var signature in RuntimeNativePrimitiveNames.PersistentSignatures)
                {
                    if (!PersistentTermNative.TryCapture(
                        runtime,
                        PersistentTermNativeNames.Module,
                        signature.Function,
                        signature.Arity,
                        out ErlangFunction body
                    ))
                        throw new InvalidOperationException(RuntimeNativePrimitiveNames.Missing);
                    bodies.Add(signature, body);
                }
                InstallNativeProvider(PersistentTermNativeNames.Module, RuntimeNativePrimitives.PersistentTermCore, bodies);
            }
            if ((selection & RuntimeNativePrimitives.EtsStorageCore) != 0)
                InstallNativeProvider(RuntimeEtsFacetNames407.Module, RuntimeNativePrimitives.EtsStorageCore,
                    RuntimeEtsCoreFacet407.CreateBodies(runtime));
        });
    }

    private void InstallNativeProvider(
        string module,
        RuntimeNativePrimitives provider,
        IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> bodies
    )
    {
        var signatures = provider == RuntimeNativePrimitives.PersistentTermCore
            ? RuntimeNativePrimitiveNames.PersistentSignatures
            : provider == RuntimeNativePrimitives.RegexCore411 ? ReFacetNames411.Signatures
            : RuntimeNativePrimitiveNames.EtsSignatures;
        if (bodies.Count != signatures.Length || bodies.Keys.Any(signature => !signatures.Contains(signature)))
            throw new InvalidOperationException(RuntimeNativePrimitiveNames.Missing);
        var existing = functions.Keys.Where(key => key.Module == module).ToArray();
        if (existing.Length != 0 || loadedModules.ContainsKey(module) || preloadedModules.ContainsKey(module)
            || sourceOwners.Keys.Any(key => key.Module == module) || oldCode.ContainsKey(module)
            || codeImages.ContainsKey(module))
        {
            bool ours = !oldCode.ContainsKey(module)
                && existing.Length == bodies.Count + RuntimeNativeProviderMetadataNames.Signatures.Length
                && existing.All(key => bodies.ContainsKey((key.Function, key.Arity))
                    ? IsCurrentNativeProvider(key, provider)
                    : RuntimeNativeProviderMetadataNames.Signatures.Contains((key.Function, key.Arity))
                        && IsCurrentNativeProviderEntry(key, provider, native: false));
            if (!ours)
                throw new InvalidOperationException(RuntimeNativePrimitiveNames.Displaced);

            return;
        }
        foreach (var body in bodies)
            if (!NativeBuiltinCatalog.Contains(module, body.Key.Function, body.Key.Arity))
                throw new InvalidOperationException(RuntimeNativePrimitiveNames.Missing);
        var information = new RuntimeNativeProviderModuleInformation(this, module);
        IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> metadata =
            new Dictionary<(string Function, int Arity), ErlangFunction>
            {
                [(BeamPreparationNames.ModuleInfo, 0)] = information.All,
                [(BeamPreparationNames.ModuleInfo, 1)] = information.Field
            };
        InstallPreloaded(registry =>
        {
            foreach (var body in metadata)
                registry.Register(module, body.Key.Function, body.Key.Arity, body.Value);
            foreach (var body in bodies)
                registry.Register(
                    module,
                    body.Key.Function,
                    body.Key.Arity,
                    body.Value
                );
        });
        foreach (var body in metadata)
        {
            var key = (Module: module, Function: body.Key.Function, Arity: body.Key.Arity);
            if (IsNativeEntry(key.Module, key.Function, key.Arity) || !ReferenceEquals(functions[key], body.Value))
                throw new InvalidOperationException(RuntimeNativePrimitiveNames.Missing);
            runtimeErrorBindings[key] = runtimeErrorBindings[key] with { Provider = provider };
        }
        foreach (var body in bodies)
        {
            var key = (Module: module, Function: body.Key.Function, Arity: body.Key.Arity);
            if (!IsNativeEntry(key.Module, key.Function, key.Arity) || !ReferenceEquals(functions[key], body.Value))
                throw new InvalidOperationException(RuntimeNativePrimitiveNames.Missing);
            runtimeErrorBindings[key] = runtimeErrorBindings[key] with { Provider = provider };
        }
    }

    private bool IsCurrentNativeProvider(
        (string Module, string Function, int Arity) signature,
        RuntimeNativePrimitives provider
    )
        => IsCurrentNativeProviderEntry(signature, provider, native: true);

    private bool IsCurrentNativeProviderEntry(
        (string Module, string Function, int Arity) signature,
        RuntimeNativePrimitives provider,
        bool native
    )
    {
        return !sourceOwners.ContainsKey(signature)
            && runtimeErrorBindings.TryGetValue(signature, out var binding)
            && binding.Provider == provider && binding.Native == native
            && functions.TryGetValue(signature, out var body) && ReferenceEquals(body, binding.Body)
            && ReferenceEquals(codeImages.GetValueOrDefault(signature.Module), binding.Image);
    }

    internal RuntimeNativePrimitiveSnapshot CaptureNativePrimitives()
    {
        lock (gate)
        {
            var bindings = new List<RuntimeNativePrimitiveBinding>();
            foreach (var entry in runtimeErrorBindings)
            {
                if (entry.Value.Provider == RuntimeNativePrimitives.None || !IsCurrentNativeProvider(entry.Key, entry.Value.Provider))
                    continue;
                var assembly = entry.Value.Body.Method.Module.Assembly;
                bindings.Add(new(
                    entry.Value.Provider,
                    entry.Key.Module,
                    entry.Key.Function,
                    entry.Key.Arity,
                    new(assembly.GetName().FullName ?? assembly.GetName().Name!, entry.Value.Body.Method.Module.ModuleVersionId)
                ));
            }

            bool Complete(string module, RuntimeNativePrimitives provider, IEnumerable<(string Function, int Arity)> signatures)
                => signatures.All(signature => bindings.Any(binding => binding.Provider == provider && binding.Module == module
                    && binding.Function == signature.Function && binding.Arity == signature.Arity));
            RuntimeNativePrimitives selection = RuntimeNativePrimitives.None;
            if (Complete(
                PersistentTermNativeNames.Module,
                RuntimeNativePrimitives.PersistentTermCore,
                RuntimeNativePrimitiveNames.PersistentSignatures
            ))
                selection |= RuntimeNativePrimitives.PersistentTermCore;
            if (Complete(RuntimeEtsFacetNames407.Module, RuntimeNativePrimitives.EtsStorageCore, RuntimeNativePrimitiveNames.EtsSignatures))
                selection |= RuntimeNativePrimitives.EtsStorageCore;

            if (Complete(ReNames411.Module, RuntimeNativePrimitives.RegexCore411, ReFacetNames411.Signatures))
                selection |= RuntimeNativePrimitives.RegexCore411;
            return new(selection, bindings);
        }
    }
}
