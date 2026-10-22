namespace Erlang;

internal static class AllocatorGatherNative434
{
    internal static bool TryCapture(
        ProcessRuntime? runtime,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (runtime is null || IntPtr.Size != sizeof(ulong) || module != AllocatorGatherNames434.Module || !AllocatorGatherNames434.Signatures.Contains((function, arity))) return false;
        body = (context, arguments) =>
        {
            if (arguments.Count != 1) throw new InvalidOperationException(AllocatorGatherNames434.Foreign);
            try
            {
                return ValueTask.FromResult(runtime.GatherAllocator434(context, arguments[0], function == AllocatorGatherNames434.Carriers));
            }
            catch (ErlangException error)
            {
                throw context.CaptureNativeFailure(
                    error,
                    module,
                    function,
                    arguments
                );
            }
        };

        return true;
    }
}
