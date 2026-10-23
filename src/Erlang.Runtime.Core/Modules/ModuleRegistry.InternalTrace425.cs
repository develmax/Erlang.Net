namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallInternalTrace425(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            bool installed = true;
            foreach (var key in InternalTraceNames425.Signatures)
            {
                var full = (InternalTraceNames425.Module, key.Function, key.Arity);
                bool same = functions.TryGetValue(full, out var body) && body.Target is InternalTraceBinding425 binding
                    && ReferenceEquals(binding.Runtime, runtime) && ReferenceEquals(body, binding.Body)
                    && !sourceOwners.ContainsKey(full) && IsNativeEntry(full.Item1, full.Item2, full.Item3);
                if (!same && (functions.ContainsKey(full) || sourceOwners.ContainsKey(full)))
                    throw new InvalidOperationException(InternalTraceNames425.Conflict);
                installed &= same;
            }
            if (installed) return;
            // Stage the complete group before atomic native registration.
            var next = InternalTraceNames425.Signatures.Select(key =>
                (Key: key, Binding: new InternalTraceBinding425(runtime, key.Function, key.Arity))).ToArray();
            InstallPreloaded(target =>
            {
                foreach (var entry in next)
                    target.Register(
                        InternalTraceNames425.Module,
                        entry.Key.Function,
                        entry.Key.Arity,
                        entry.Binding.Body
                    );
            });
        });
    }
}
