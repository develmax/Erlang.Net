namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallDistChannel429(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (DistChannel429Names.Module,
                DistChannel429Names.Function, DistChannel429Names.Arity);
            if (functions.TryGetValue(key, out var body)
                && body.Target is DistChannel429Binding binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                throw new InvalidOperationException(DistChannel429Names.Conflict);
            var next = new DistChannel429Binding(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(DistChannel429Names.Conflict);
        });
    }
}
