namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallBootstrapAtomics413(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(registry =>
        {
            var key = (AtomicsNames413.Internal, AtomicsNames413.New, 2);
            if (functions.TryGetValue(key, out ErlangFunction? body)
                && body.Target is AtomicsConstructorBinding413 existing && ReferenceEquals(existing.Runtime, runtime)
                && ReferenceEquals(existing.Body, body) && !sourceOwners.ContainsKey(key)
                && IsNativeEntry(key.Item1, key.Item2, key.Item3)) return;
            if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)) throw new InvalidOperationException(AtomicsNames413.Conflict);
            var binding = new AtomicsConstructorBinding413(runtime, this);
            InstallPreloaded(target => target.Register(
                key.Item1,
                key.Item2,
                key.Item3,
                binding.Body
            ));
            if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], binding.Body))
                throw new InvalidOperationException(AtomicsNames413.Conflict);
        });
    }
}
