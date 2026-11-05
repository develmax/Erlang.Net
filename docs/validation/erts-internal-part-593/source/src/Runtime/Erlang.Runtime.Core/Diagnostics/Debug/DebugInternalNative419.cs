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
        body = async (context, args) =>
        {
            owner.RequireDebugInternalActor419(context);
            if (args.Count != arity) throw new ArgumentException(DebugInternalNames419.Foreign);
            try
            {
                if (function == DebugInternalNames419.Get && (args[0] is Atom { Name: NodeDistReferenceNames.Query }
                    || args[0] is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom { Name: NodeDistReferenceNames.Query }))
                    return await owner.ReadNodeDistReferences(context, args[0]);

                return function == DebugInternalNames419.Get ? owner.ReadDebugInternal419(context, args[0]) : owner.WriteDebugInternal419(context, args[0], args[1]);
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
