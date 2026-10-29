namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void RequireRegexRegistrationCaller411() => RequireAsyncSourceCaller();

    internal void InstallRegexProvider411(ProcessRuntime runtime, RuntimeRegexEngine411 engine)
    {
        if (!ReferenceEquals(owningRuntime, runtime) || !ReferenceEquals(engine.Owner413, runtime)) throw new ArgumentException(ReNames411.Foreign);
        runtime.RequireNativePrimitiveRegistration();
        var bodies = engine.CreateBodies();
        using var source = EnterSourceMutation();
        Apply(_ =>
        {
            InstallNativeProvider(ReNames411.Module, RuntimeNativePrimitives.RegexCore411, bodies);
            if (runtime.OwnedTermsPilot506)
                foreach (var signature in ReFacetNames411.Signatures)
                {
                    var key = (Module: ReNames411.Module, signature.Function, signature.Arity);
                    if (!IsCurrentNativeProviderEntry(key, RuntimeNativePrimitives.RegexCore411, native: true)
                        || sourceOwners.ContainsKey(key)
                        || functions[key].Target is not ReNativeBinding413 binding || !ReferenceEquals(binding.Engine, engine))
                        throw new InvalidOperationException(ReMagicNames597.Invalid);
                    // Exact body and image captured once by the existing policy.
                    // Public replacement/re-registration clears this provenance.
                    ownedBindings507[key] = new(
                        functions[key],
                        codeImages[key.Module],
                        null,
                        OwnedBindingKind507.RuntimeRead535
                    );
                }
        });
    }
}
