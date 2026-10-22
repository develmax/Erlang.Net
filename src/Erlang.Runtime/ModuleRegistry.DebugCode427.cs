namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallDebugCode427(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            bool installed = true;
            foreach (var signature in DebugCodeNames427.Signatures)
            {
                var key = (DebugCodeNames427.Module, signature.Function, signature.Arity);
                bool same = functions.TryGetValue(key, out ErlangFunction? body) && body.Target is DebugCodeBinding427 binding
                    && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3);
                if (!same && (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))) throw new InvalidOperationException(DebugCodeNames427.Conflict);
                installed &= same;
            }
            if (installed) return;
            var staged = DebugCodeNames427.Signatures.Select(signature =>
                (Signature: signature, Binding: new DebugCodeBinding427(runtime, signature.Function, signature.Arity))).ToArray();
            InstallPreloaded(registry =>
            {
                foreach (var entry in staged)
                    registry.Register(
                        DebugCodeNames427.Module,
                        entry.Signature.Function,
                        entry.Signature.Arity,
                        entry.Binding.Body
                    );
            });
        });
    }
}
