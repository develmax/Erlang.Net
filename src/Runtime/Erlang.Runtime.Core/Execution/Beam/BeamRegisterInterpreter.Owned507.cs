namespace Erlang;

internal static partial class BeamRegisterInterpreter
{
    // This adapter is created only in the opt-in owned lane. The helper checks
    // the original delegate's provenance, not the adapter or a module name.
    private static ErlangFunction OwnedPrimitive507(BeamExecutionIdentity execution, ErlangFunction primitive)
        => (context, arguments) => context.InvokeNativePrimitive507(execution, primitive, arguments);

    private static void DisposeOwnedFrames507(
        BeamRegisterState registers,
        BeamFunctionScope? current,
        Stack<BeamFunctionScope> suspended
    )
    {
        List<Exception>? failures = null;

        void Dispose(IDisposable? value)
        {
            try
            {
                value?.Dispose();
            }
            catch (Exception failure)
            {
                (failures ??= new()).Add(failure);
            }
        }
        Dispose(registers);
        Dispose(current);
        while (suspended.TryPop(out var frame))
            Dispose(frame);
        if (failures is not null)
            throw new AggregateException(failures);
    }
}
