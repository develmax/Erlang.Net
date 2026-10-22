namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallDebugDispatch432(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            bool installed = true;
            foreach (var signature in DebugDispatchNames432.Signatures)
            {
                var key = (DebugDispatchNames432.Module, signature.Function, signature.Arity);
                bool same = functions.TryGetValue(key, out ErlangFunction? body) && body.Target is DebugDispatchBinding432 binding
                    && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3);
                if (!same && (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)))
                    throw new InvalidOperationException(DebugDispatchNames432.Conflict);
                installed &= same;
            }
            if (installed) return;
            var staged = DebugDispatchNames432.Signatures.Select(signature =>
                (Signature: signature, Binding: new DebugDispatchBinding432(runtime, signature.Function, signature.Arity))).ToArray();
            InstallPreloaded(registry =>
            {
                foreach (var entry in staged)
                    registry.Register(
                        DebugDispatchNames432.Module,
                        entry.Signature.Function,
                        entry.Signature.Arity,
                        entry.Binding.Body
                    );
            });
        });
    }
}
