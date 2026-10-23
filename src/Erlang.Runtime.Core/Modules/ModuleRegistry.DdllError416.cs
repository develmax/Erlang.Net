namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallDdllError416(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (DdllErrorNames416.ModuleName, DdllErrorNames416.Function, 1);
            if (functions.TryGetValue(key, out var body) && body.Target is DdllErrorBinding416 binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(binding.Body, body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                throw new InvalidOperationException(DdllErrorNames416.Conflict);
            var next = new DdllErrorBinding416(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(DdllErrorNames416.Conflict);
        });
    }
}
