namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapScheduler(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            var key = (RuntimeBootstrapSchedulerNames.Module, RuntimeBootstrapSchedulerNames.Function,
                RuntimeBootstrapSchedulerNames.Arity);
            if (functions.TryGetValue(key, out ErlangFunction? body)
                && body.Target is RuntimeBootstrapSchedulerBinding existing
                && ReferenceEquals(existing.Runtime, runtime) && ReferenceEquals(existing.Body, body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3))
                return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                throw new InvalidOperationException(RuntimeBootstrapSchedulerNames.Conflict);
            var binding = new RuntimeBootstrapSchedulerBinding(runtime, this);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                binding.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3)
                || !ReferenceEquals(functions[key], binding.Body))
                throw new InvalidOperationException(RuntimeBootstrapSchedulerNames.Conflict);
        });
    }
}
