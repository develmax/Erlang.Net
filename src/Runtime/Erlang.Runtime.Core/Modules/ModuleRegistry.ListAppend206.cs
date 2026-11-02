namespace Erlang;

public sealed partial class ModuleRegistry
{
    // Exact canonical allocating primitive only; public Register stays opaque.
    internal void RegisterCanonicalListAppend206(string function)
    {
        if (function is not (ErlangOperators.Append or ErlangBasicBifNames.Append))
            throw new ArgumentException(nameof(function));
        ErlangFunction body = ErlangListAppend206.Invoke;
        Register(
            ErlangModuleNames.Module,
            function,
            ErlangListAppend206Names.Arity,
            body
        );
        lock (gate)
        {
            var key = (ErlangModuleNames.Module, function, ErlangListAppend206Names.Arity);
            if (owningRuntime is not null && functions.TryGetValue(key, out var selected)
                && ReferenceEquals(selected, body) && !sourceOwners.ContainsKey(key))
                ownedBindings507[key] = new(
                    body,
                    codeImages[ErlangModuleNames.Module],
                    null,
                    OwnedBindingKind507.LocalListAppend
                );
        }
    }
}
