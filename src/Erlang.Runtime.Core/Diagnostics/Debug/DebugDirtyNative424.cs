namespace Erlang;

internal static class DebugDirtyNative424
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || IntPtr.Size != sizeof(ulong) || module != DebugDirtyNames424.Module || !DebugDirtyNames424.Signatures.Contains((function, arity)))
            return false;
        body = (context, arguments) => owner.ExecuteDebugDirty424(context, function, arguments);

        return true;
    }
}
