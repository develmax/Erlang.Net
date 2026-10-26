namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapEtsAll420(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            var key = (EtsAllNames420.Module, EtsAllNames420.Function, 0);
            if (functions.TryGetValue(key, out ErlangFunction? body) && body.Target is EtsAllBinding420 existing
       && ReferenceEquals(existing.Runtime, runtime) && ReferenceEquals(existing.Body, body) && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(EtsAllNames420.Conflict);
            var binding = new EtsAllBinding420(runtime, this);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                binding.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], binding.Body)) throw new InvalidOperationException(EtsAllNames420.Conflict);
        });
    }
}
