namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallInternalPorts416(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            foreach (var signature in InternalPortNames416.Signatures)
            {
                var key = (InternalPortNames416.Module, signature.Function, signature.Arity);
                if (functions.TryGetValue(key, out ErlangFunction? body)
                    && body.Target is InternalPortBinding416 existing
                    && ReferenceEquals(existing.Runtime, runtime)
                    && ReferenceEquals(existing.Body, body)
                    && !sourceOwners.ContainsKey(key)
                    && IsNativeEntry(key.Item1, key.Item2, key.Item3))
                    continue;
                if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                    throw new InvalidOperationException(InternalPortNames416.Conflict);
                var binding = new InternalPortBinding416(runtime, signature.Function, signature.Arity);
                InstallPreloaded(target => target.Register(
                    key.Item1,
                    key.Item2,
                    key.Item3,
                    binding.Body
                ));
                if (!IsNativeEntry(key.Item1, key.Item2, key.Item3)
                    || !ReferenceEquals(functions[key], binding.Body))
                    throw new InvalidOperationException(InternalPortNames416.Conflict);
            }
        });
    }
}
