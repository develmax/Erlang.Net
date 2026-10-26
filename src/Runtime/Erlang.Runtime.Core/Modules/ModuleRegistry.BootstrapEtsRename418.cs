namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapEtsRename418(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            var key = (EtsRenameNames418.Module, EtsRenameNames418.Rename, EtsRenameNames418.Arity);
            if (functions.TryGetValue(key, out ErlangFunction? body) && body.Target is EtsRenameBinding418 existing
                && ReferenceEquals(existing.Runtime, runtime) && ReferenceEquals(existing.Body, body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(EtsRenameNames418.Conflict);
            var binding = new EtsRenameBinding418(runtime, this);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                binding.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], binding.Body))
                throw new InvalidOperationException(EtsRenameNames418.Conflict);
        });
    }
}
