namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallDebugDirty424(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            bool installed = true;
            foreach (var key in DebugDirtyNames424.Signatures)
            {
                var full = (DebugDirtyNames424.Module, key.Function, key.Arity);
                bool same = functions.TryGetValue(full, out var body) && body.Target is DebugDirtyBinding424 binding
                    && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                    && !sourceOwners.ContainsKey(full) && IsNativeEntry(full.Item1, full.Item2, full.Item3);
                if (!same && (functions.ContainsKey(full) || sourceOwners.ContainsKey(full)))
                    throw new InvalidOperationException(DebugDirtyNames424.Conflict);
                installed &= same;
            }
            if (installed) return;
            // Stage the complete group before atomic native registration.
            var next = DebugDirtyNames424.Signatures.Select(key =>
                (Key: key, Binding: new DebugDirtyBinding424(runtime, key.Function, key.Arity))).ToArray();
            InstallPreloaded(target =>
            {
                foreach (var entry in next)
                    target.Register(
                        DebugDirtyNames424.Module,
                        entry.Key.Function,
                        entry.Key.Arity,
                        entry.Binding.Body
                    );
            });
        });
    }
}
