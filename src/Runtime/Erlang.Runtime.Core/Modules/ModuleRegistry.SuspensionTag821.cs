namespace Erlang;

public sealed partial class ModuleRegistry
{
    // Installer-only capability; names, public Host overrides and arbitrary
    // function registration do not acquire a genuine guest borrow.
    internal void RegisterCanonicalSuspension821(
        string module,
        string function,
        int arity,
        ErlangFunction body
    )
    {
        Register(
            module,
            function,
            arity,
            body
        );
        lock (gate)
        {
            var key = (module, function, arity);
            if (owningRuntime is not null && functions.TryGetValue(key, out var selected)
                && ReferenceEquals(selected, body) && !sourceOwners.ContainsKey(key))
                ownedBindings507[key] = new(
                    body,
                    codeImages[module],
                    null,
                    OwnedBindingKind507.SuspensionTag821
                );
        }
    }
}
