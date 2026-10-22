namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallDistributionProfile429(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (InternalDistributionProfile429Names.Module,
                InternalDistributionProfile429Names.Function, InternalDistributionProfile429Names.Arity);
            if (functions.TryGetValue(key, out var body)
                && body.Target is InternalDistributionProfile429Binding binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                throw new InvalidOperationException(InternalDistributionProfile429Names.Conflict);
            var next = new InternalDistributionProfile429Binding(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(InternalDistributionProfile429Names.Conflict);
        });
    }
}
