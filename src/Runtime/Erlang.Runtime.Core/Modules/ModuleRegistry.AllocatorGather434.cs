namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallAllocatorGather434(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            bool installed = true;
            foreach (var signature in AllocatorGatherNames434.Signatures)
            {
                var key = (AllocatorGatherNames434.Module, signature.Function, signature.Arity);
                bool same = functions.TryGetValue(key, out ErlangFunction? body) && body.Target is AllocatorGatherBinding434 binding
                    && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3);
                if (!same && (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)))
                    throw new InvalidOperationException(AllocatorGatherNames434.Conflict);
                installed &= same;
            }
            if (installed) return;
            var staged = AllocatorGatherNames434.Signatures.Select(signature =>
                (Signature: signature, Binding: new AllocatorGatherBinding434(runtime, signature.Function, signature.Arity))).ToArray();
            InstallPreloaded(registry =>
            {
                foreach (var entry in staged)
                    registry.Register(
                        AllocatorGatherNames434.Module,
                        entry.Signature.Function,
                        entry.Signature.Arity,
                        entry.Binding.Body
                    );
            });
        });
    }
}
