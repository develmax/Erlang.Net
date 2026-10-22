namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallProcessAlive425(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (ProcessAliveNames425.Module, ProcessAliveNames425.Function, ProcessAliveNames425.Arity);
            if (functions.TryGetValue(key, out var body) && body.Target is ProcessAliveBinding425 binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(ProcessAliveNames425.Conflict);
            var next = new ProcessAliveBinding425(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(ProcessAliveNames425.Conflict);
        });
    }
}
