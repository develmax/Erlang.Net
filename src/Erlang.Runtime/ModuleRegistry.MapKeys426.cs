namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallMapKeys426(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (MapKeysNames426.Module, MapKeysNames426.Function, MapKeysNames426.Arity);
            if (functions.TryGetValue(key, out var body) && body.Target is MapKeysBinding426 binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3))
                return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                throw new InvalidOperationException(MapKeysNames426.Conflict);
            var next = new MapKeysBinding426(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(MapKeysNames426.Conflict);
        });
    }
}
