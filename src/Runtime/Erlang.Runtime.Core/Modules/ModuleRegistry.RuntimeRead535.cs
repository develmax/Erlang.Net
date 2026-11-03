namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void RegisterCanonicalRuntimeRead535(
        string module,
        string function,
        int arity,
        ErlangFunction body
    )
    {
        using var modification = CodeModification.Enter();
        lock (gate)
        {
            Register(
                module,
                function,
                arity,
                body
            );
            var key = (module, function, arity);
            if (owningRuntime is not null && functions.TryGetValue(key, out var selected)
                && ReferenceEquals(selected, body) && !sourceOwners.ContainsKey(key))
                ownedBindings507[key] = new(
                    body,
                    codeImages[module],
                    null,
                    OwnedBindingKind507.RuntimeRead535
                );
        }
    }
}
