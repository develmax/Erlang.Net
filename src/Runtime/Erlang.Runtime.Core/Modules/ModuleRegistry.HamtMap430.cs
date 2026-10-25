namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallHamtMap430(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            bool installed = true;
            foreach (var key in HamtNativeNames430.Signatures)
            {
                bool same = functions.TryGetValue(key, out var body) && body.Target is HamtMapBinding430 binding
                    && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Module, key.Function, key.Arity);
                if (!same && (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)))
                    throw new InvalidOperationException(HamtNativeNames430.Conflict);
                installed &= same;
            }
            if (installed) return;
            var staged = HamtNativeNames430.Signatures.Select(key =>
                (Key: key, Binding: new HamtMapBinding430(
                    runtime,
                    key.Module,
                    key.Function,
                    key.Arity
                ))).ToArray();
            InstallPreloaded(target =>
            {
                foreach (var row in staged) target.Register(
                    row.Key.Module,
                    row.Key.Function,
                    row.Key.Arity,
                    row.Binding.Body
                );
            });
        });
    }
}
