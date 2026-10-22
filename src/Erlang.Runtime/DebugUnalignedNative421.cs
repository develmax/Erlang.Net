namespace Erlang;

internal static class DebugUnalignedNative421
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
        if (owner is null || module != DebugUnaligned421Names.Module
            || function != DebugUnaligned421Names.Function || arity != 2)
            return false;
        body = (context, arguments) => ValueTask.FromResult<Term>(owner.ReadDebugUnaligned421(context, arguments));

        return true;
    }
}
