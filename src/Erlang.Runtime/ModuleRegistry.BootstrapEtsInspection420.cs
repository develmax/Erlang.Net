namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapEtsInspection420(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            foreach (var signature in EtsInspectionNames420.Signatures)
            {
                var key = (EtsInspectionNames420.Module, signature.Function, signature.Arity);
                if (functions.TryGetValue(key, out ErlangFunction? body) && body.Target is EtsInspectionBinding420 existing
                    && ReferenceEquals(existing.Runtime, runtime) && ReferenceEquals(existing.Body, body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3)) continue;
                if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(EtsInspectionNames420.Conflict);
                var binding = new EtsInspectionBinding420(runtime, this, signature);
                InstallPreloaded(target => target.Register(
                    key.Item1,
                    key.Item2,
                    key.Item3,
                    binding.Body
                ));
                if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], binding.Body))
                    throw new InvalidOperationException(EtsInspectionNames420.Conflict);
            }
        });
    }
}
