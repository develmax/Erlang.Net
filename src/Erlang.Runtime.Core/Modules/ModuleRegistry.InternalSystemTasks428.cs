namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallInternalSystemTasks428(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            foreach (var signature in SystemTaskNames428.Signatures)
            {
                var key = (SystemTaskNames428.Module, signature.Function, signature.Arity);
                if (functions.TryGetValue(key, out var body))
                {
                    bool authentic = body.Target is InternalSystemTaskBinding428 binding
                        && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(binding.Body, body)
                        && binding.Function == signature.Function && binding.Arity == signature.Arity;
                    authentic |= signature.Function == InternalGarbageCollectionNames428.Function
                        && body.Target is InternalGarbageCollectionBinding428 gc
                        && ReferenceEquals(gc.Runtime, runtime) && ReferenceEquals(gc.Body, body);
                    if (authentic && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) continue;
                    throw new InvalidOperationException(InternalGarbageCollectionNames428.Conflict);
                }
                if (sourceOwners.ContainsKey(key)) throw new InvalidOperationException(InternalGarbageCollectionNames428.Conflict);
                ErlangFunction next = signature.Function == InternalGarbageCollectionNames428.Function
                    ? new InternalGarbageCollectionBinding428(runtime).Body
                    : new InternalSystemTaskBinding428(runtime, signature.Function, signature.Arity).Body;
                InstallPreloaded(target => target.Register(
                    key.Item1,
                    key.Item2,
                    key.Item3,
                    next
                ));
                if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next))
                    throw new InvalidOperationException(InternalGarbageCollectionNames428.Conflict);
            }
        });
    }
}
