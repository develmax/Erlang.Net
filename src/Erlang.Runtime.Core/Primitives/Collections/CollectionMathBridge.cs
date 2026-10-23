namespace Erlang;

internal static class CollectionMathBridge
{
    internal static void Register(ModuleRegistry registry)
        => registry.Register(
            CollectionMathNames.Module,
            CollectionMathNames.Log,
            1,
            (_, arguments) => ValueTask.FromResult(MathNative.Unary(MathNativeNames.Log, arguments[0]))
        );
}
