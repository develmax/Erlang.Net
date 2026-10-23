namespace Erlang;

internal static class DebugSharingNative425
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
        if (runtime is null || IntPtr.Size != sizeof(ulong) || module != DebugSharingNames425.Module
            || !DebugSharingNames425.Signatures.Contains((function, arity))) return false;
        body = (context, arguments) => ValueTask.FromResult(runtime.ExecuteDebugSharing425(context, function, arguments));

        return true;
    }
}
