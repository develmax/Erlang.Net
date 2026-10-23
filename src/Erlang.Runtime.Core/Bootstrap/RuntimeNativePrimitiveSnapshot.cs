namespace Erlang;

public sealed class RuntimeNativePrimitiveSnapshot
{
    internal RuntimeNativePrimitiveSnapshot(
        RuntimeNativePrimitives selection,
        IEnumerable<RuntimeNativePrimitiveBinding> bindings
    )
    {
        Selection = selection;
        Bindings = Array.AsReadOnly(bindings.OrderBy(item => item.Module, StringComparer.Ordinal)
            .ThenBy(item => item.Function, StringComparer.Ordinal).ThenBy(item => item.Arity).ToArray());
    }

    public RuntimeNativePrimitives Selection { get; }

    public IReadOnlyList<RuntimeNativePrimitiveBinding> Bindings { get; }
}
