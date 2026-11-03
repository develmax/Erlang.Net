namespace Erlang;

public sealed partial class ModuleRegistry
{
    private void RegisterCanonicalDebugCopy377(DebugSharingBinding425 binding)
    {
        ErlangFunction body = binding.Body;
        Register(
            DebugSharingNames425.Module,
            DebugSharingNames425.Copy,
            2,
            body
        );
        lock (gate)
        {
            var key = (DebugSharingNames425.Module, DebugSharingNames425.Copy, 2);
            if (owningRuntime is not null && ReferenceEquals(binding.Runtime, owningRuntime)
                && functions.TryGetValue(key, out var selected) && ReferenceEquals(selected, body)
                && !sourceOwners.ContainsKey(key))
                ownedBindings507[key] = new(
                    body,
                    codeImages[key.Item1],
                    null,
                    OwnedBindingKind507.LocalDebugCopy377
                );
        }
    }
}
