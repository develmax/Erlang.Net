namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void RegisterCanonicalListAppend(
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
                    OwnedBindingKind507.LocalListAppend
                );
        }
    }

}
