namespace Erlang;

internal static class CollectionMathBridge
{
    internal static void Register(ModuleRegistry registry)
    {
        foreach (string function in MathNativeNames.UnaryFunctions)
        {
            MathNative.TryCapture(
                MathNativeNames.Module,
                function,
                1,
                out ErlangFunction body
            );
            registry.Register(
                MathNativeNames.Module,
                function,
                1,
                body
            );
        }
        foreach (string function in MathNativeNames.BinaryFunctions)
        {
            MathNative.TryCapture(
                MathNativeNames.Module,
                function,
                2,
                out ErlangFunction body
            );
            registry.Register(
                MathNativeNames.Module,
                function,
                2,
                body
            );
        }
        registry.Register(
            MathNativeNames.Module,
            MathNativeNames.Pi,
            0,
            (_, _) => ValueTask.FromResult<Term>(new FloatTerm(Math.PI))
        );
        registry.Register(
            MathNativeNames.Module,
            MathNativeNames.Tau,
            0,
            (_, _) => ValueTask.FromResult<Term>(new FloatTerm(Math.Tau))
        );
    }
}
