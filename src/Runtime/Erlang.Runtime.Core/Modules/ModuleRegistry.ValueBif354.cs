namespace Erlang;

public sealed partial class ModuleRegistry
{
    // Exact reviewed runtime bodies only. Public Register remains opaque.
    internal void RegisterCanonicalValueBif354(
        string function,
        int arity,
        Func<ProcessContext, IReadOnlyList<Term>, Term> evaluate
    )
    {
        var binding = new CanonicalValueBif354(evaluate);
        ErlangFunction body = binding.Body;
        Register(
            ErlangModuleNames.Module,
            function,
            arity,
            body
        );
        lock (gate)
        {
            var key = (ErlangModuleNames.Module, function, arity);
            if (owningRuntime is not null && functions.TryGetValue(key, out var selected)
                && ReferenceEquals(selected, body) && !sourceOwners.ContainsKey(key))
                ownedBindings507[key] = new(
                    body,
                    codeImages[ErlangModuleNames.Module],
                    null,
                    OwnedBindingKind507.LocalValueBif354
                );
        }
    }
}
