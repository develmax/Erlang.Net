namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallCheckProcessCode423(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (CheckProcessCodeNames423.Module, CheckProcessCodeNames423.Function, CheckProcessCodeNames423.Arity);
            if (functions.TryGetValue(key, out var body) && body.Target is CheckProcessCodeBinding423 binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(CheckProcessCodeNames423.Conflict);
            var next = new CheckProcessCodeBinding423(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(CheckProcessCodeNames423.Conflict);
        });
    }
}
