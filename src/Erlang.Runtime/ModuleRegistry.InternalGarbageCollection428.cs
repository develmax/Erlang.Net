namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallInternalGarbageCollection428(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            var key = (InternalGarbageCollectionNames428.Module,
                InternalGarbageCollectionNames428.Function, InternalGarbageCollectionNames428.Arity);
            if (functions.TryGetValue(key, out var body)
                && body.Target is InternalGarbageCollectionBinding428 binding
                && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                throw new InvalidOperationException(InternalGarbageCollectionNames428.Conflict);
            var next = new InternalGarbageCollectionBinding428(runtime);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                next.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                throw new InvalidOperationException(InternalGarbageCollectionNames428.Conflict);
        });
    }
}
