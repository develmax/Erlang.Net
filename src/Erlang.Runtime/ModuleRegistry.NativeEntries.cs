namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal bool IsNativeEntry(string module, string function, int arity)
    {
        lock (gate)
        {
            var signature = (module, function, arity);

            return !sourceOwners.ContainsKey(signature)
                && runtimeErrorBindings.TryGetValue(signature, out var binding)
                && binding.Native
                && functions.TryGetValue(signature, out var body)
                && ReferenceEquals(binding.Body, body)
                && ReferenceEquals(binding.Image, codeImages.GetValueOrDefault(module));
        }
    }
}
