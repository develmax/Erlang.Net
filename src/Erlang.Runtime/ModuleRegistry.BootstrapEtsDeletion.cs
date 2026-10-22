namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapEtsDeletion(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            foreach (string function in RuntimeEtsSelectDeleteNames412.Functions)
            {
                var key = (RuntimeEtsSelectDeleteNames412.Module, function, 2);
                if (functions.TryGetValue(key, out ErlangFunction? body)
                    && body.Target is RuntimeBootstrapEtsDeletionBinding existing
                    && ReferenceEquals(existing.Runtime, runtime) && ReferenceEquals(existing.Body, body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3))
                    continue;
                if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                    throw new InvalidOperationException(RuntimeEtsSelectDeleteNames412.Conflict);
                var binding = new RuntimeBootstrapEtsDeletionBinding(runtime, this, function);
                InstallPreloaded(target => target.Register(
                    key.Item1,
                    key.Item2,
                    key.Item3,
                    binding.Body
                ));
                if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], binding.Body))
                    throw new InvalidOperationException(RuntimeEtsSelectDeleteNames412.Conflict);
            }
        });
    }
}
