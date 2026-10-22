namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBeamfileMd5423(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (BeamfileMd5Names423.Module, BeamfileMd5Names423.Function, BeamfileMd5Names423.Arity);
            if (functions.TryGetValue(key, out var body) && body.Target is BeamfileMd5Binding423 binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(BeamfileMd5Names423.Conflict);
            var next = new BeamfileMd5Binding423(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(BeamfileMd5Names423.Conflict);
        });
    }
}
