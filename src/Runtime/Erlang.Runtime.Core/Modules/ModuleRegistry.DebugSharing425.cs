namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallDebugSharing425(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            bool installed = true;
            foreach (var signature in DebugSharingNames425.Signatures)
            {
                var key = (DebugSharingNames425.Module, signature.Function, signature.Arity);
                bool same = functions.TryGetValue(key, out ErlangFunction? body) && body.Target is DebugSharingBinding425 binding
                    && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                    && !sourceOwners.ContainsKey(key) && IsNativeEntry(key.Item1, key.Item2, key.Item3);
                if (!same && (functions.ContainsKey(key) || sourceOwners.ContainsKey(key)))
                    throw new InvalidOperationException(DebugSharingNames425.Conflict);
                installed &= same;
            }
            if (installed) return;
            var staged = DebugSharingNames425.Signatures.Select(signature =>
                (Signature: signature, Binding: new DebugSharingBinding425(runtime, signature.Function, signature.Arity))).ToArray();
            InstallPreloaded(registry =>
            {
                foreach (var entry in staged)
                {
                    if (entry.Signature.Function is DebugSharingNames425.Flat or DebugSharingNames425.Shared)
                        registry.RegisterCanonicalRead507(
                            DebugSharingNames425.Module,
                            entry.Signature.Function,
                            entry.Signature.Arity,
                            entry.Binding.Body
                        );
                    else
                        registry.Register(
                            DebugSharingNames425.Module,
                            entry.Signature.Function,
                            entry.Signature.Arity,
                            entry.Binding.Body
                        );
                }
            });
        });
    }
}
