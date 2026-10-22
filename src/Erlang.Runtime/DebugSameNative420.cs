namespace Erlang;

internal static class DebugSameNative420
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
        if (owner is null || module != DebugSame420Names.Module || function != DebugSame420Names.Function || arity != 2) return false;
        body = (context, arguments) =>
        {
            return ValueTask.FromResult(CoreModules.Boolean(owner.ReadDebugSame420(context, arguments)));
        };

        return true;
    }
}
