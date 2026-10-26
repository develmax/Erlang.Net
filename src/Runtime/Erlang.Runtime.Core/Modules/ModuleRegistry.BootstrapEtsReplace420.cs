namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapEtsReplace420(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            var key = (EtsReplaceNames420.Module, EtsReplaceNames420.Function, EtsReplaceNames420.Arity);
            if (functions.TryGetValue(key, out ErlangFunction? body) && body.Target is EtsReplaceBinding420 existing
                && ReferenceEquals(existing.Runtime, runtime) && ReferenceEquals(existing.Body, body)
                && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(EtsReplaceNames420.Conflict);
            var binding = new EtsReplaceBinding420(runtime, this);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                binding.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], binding.Body))
                throw new InvalidOperationException(EtsReplaceNames420.Conflict);
        });
    }
}
