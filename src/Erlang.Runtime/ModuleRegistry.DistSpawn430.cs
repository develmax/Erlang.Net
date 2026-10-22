namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallDistSpawn430(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (DistSpawn430Names.Module,
                DistSpawn430Names.Function, DistSpawn430Names.Arity);
            if (functions.TryGetValue(key, out var body)
                && body.Target is DistSpawn430Binding binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                throw new InvalidOperationException(DistSpawn430Names.Conflict);
            var next = new DistSpawn430Binding(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(DistSpawn430Names.Conflict);
        });
    }
}
