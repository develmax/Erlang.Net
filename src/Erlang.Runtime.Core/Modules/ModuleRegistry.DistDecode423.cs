namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallDistDecode423(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (DistDecodeNames423.Module, DistDecodeNames423.Function, DistDecodeNames423.Arity);
            if (functions.TryGetValue(key, out var body) && body.Target is DistDecodeBinding423 binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(DistDecodeNames423.Conflict);
            var next = new DistDecodeBinding423(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(DistDecodeNames423.Conflict);
        });
    }
}
