namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void InstallInternalScalars413(ProcessRuntime runtime)
    {
        using var mutation = EnterSourceMutation();
        Apply(_ =>
        {
            foreach (var signature in ErtsInternalScalarNativeNames413.Signatures)
            {
                var key = (ErtsInternalScalarNativeNames413.Module, signature.Function, signature.Arity);
                if (functions.TryGetValue(key, out var body)
                    && body.Target is InternalScalarBinding413 binding && ReferenceEquals(binding.Runtime, runtime)
                    && ReferenceEquals(body, binding.Body) && !sourceOwners.ContainsKey(key)
                    && IsNativeEntry(key.Item1, key.Item2, key.Item3))
                    continue;
                if (functions.ContainsKey(key) || sourceOwners.ContainsKey(key))
                    throw new InvalidOperationException(ErtsInternalScalarNativeNames413.Conflict);
                var next = new InternalScalarBinding413(runtime, signature.Function, signature.Arity);
                InstallPreloaded(target => target.Register(
                    key.Item1,
                    key.Item2,
                    key.Item3,
                    next.Body
                ));
                if (!IsNativeEntry(key.Item1, key.Item2, key.Item3) || !ReferenceEquals(functions[key], next.Body))
                    throw new InvalidOperationException(ErtsInternalScalarNativeNames413.Conflict);
            }
        });
    }
}
