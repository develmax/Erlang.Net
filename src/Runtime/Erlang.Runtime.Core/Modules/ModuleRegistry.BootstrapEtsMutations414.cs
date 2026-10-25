namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapEtsMutations414(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            foreach (var signature in EtsMutationNames414.Signatures)
            {
                var key = (EtsMutationNames414.Module, signature.Function, signature.Arity);
                if (functions.TryGetValue(key, out ErlangFunction? body) && body.Target is EtsMutationBinding414 existing
                    && ReferenceEquals(existing.Runtime, runtime) && ReferenceEquals(existing.Body, body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) continue;
                if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(EtsMutationNames414.Conflict);
                var binding = new EtsMutationBinding414(runtime, this, signature);
                InstallPreloaded(target => target.Register(
                    key.Item1,
                    key.Item2,
                    key.Item3,
                    binding.Body
                ));
                if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], binding.Body))
                    throw new InvalidOperationException(EtsMutationNames414.Conflict);
            }
        });
    }
}
