namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapEtsQueries414(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            foreach (string function in EtsQueryNames414.Functions)
            {
                var key = (EtsQueryNames414.Module, function, 2);
                if (functions.TryGetValue(key, out ErlangFunction? body) && body.Target is EtsQueryBinding414 existing
                    && ReferenceEquals(existing.Runtime, runtime) && ReferenceEquals(existing.Body, body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) continue;
                if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(EtsQueryNames414.Conflict);
                var binding = new EtsQueryBinding414(runtime, this, function);
                InstallPreloaded(target => target.Register(
                    key.Item1,
                    key.Item2,
                    key.Item3,
                    binding.Body
                ));
                if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], binding.Body))
                    throw new InvalidOperationException(EtsQueryNames414.Conflict);
            }
        });
    }
}
