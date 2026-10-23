namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapEtsCounter417(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            foreach (var signature in EtsCounterNames417.Signatures)
            {
                var key = (EtsCounterNames417.Module, signature.Function, signature.Arity);
                if (functions.TryGetValue(key, out ErlangFunction? body) && body.Target is EtsCounterBinding417 existing
                    && ReferenceEquals(existing.Runtime, runtime) && ReferenceEquals(existing.Body, body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) continue;
                if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(EtsCounterNames417.Conflict);
                var binding = new EtsCounterBinding417(runtime, this, signature);
                InstallPreloaded(target => target.Register(
                    key.Item1,
                    key.Item2,
                    key.Item3,
                    binding.Body
                ));
                if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], binding.Body))
                    throw new InvalidOperationException(EtsCounterNames417.Conflict);
            }
        });
    }
}
