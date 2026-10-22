namespace Erlang;

internal static class DebugInternalNative419
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
        if (owner is null || module != DebugInternalNames419.Module || !DebugInternalNames419.Signatures.Contains((function, arity))) return false;
        body = (context, args) =>
        {
            owner.RequireDebugInternalActor419(context);
            if (args.Count != arity) throw new ArgumentException(DebugInternalNames419.Foreign);
            try
            {
                return ValueTask.FromResult(function == DebugInternalNames419.Get ? owner.ReadDebugInternal419(context, args[0]) : owner.WriteDebugInternal419(context, args[0], args[1]));
            }
            catch (ErlangException error)
            {
                throw context.CaptureNativeFailure(
                    error,
                    module,
                    function,
                    args
                );
            }
        };

        return true;
    }
}
