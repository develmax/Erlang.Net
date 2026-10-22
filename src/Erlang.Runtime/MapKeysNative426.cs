namespace Erlang;

internal static class MapKeysNative426
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
        if (owner is null || module != MapKeysNames426.Module || function != MapKeysNames426.Function || arity != MapKeysNames426.Arity)
            return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != MapKeysNames426.Arity)
                throw new InvalidOperationException(MapKeysNames426.Foreign);
            owner.RequireBeamfileActor418(context);
            try
            {
                if (arguments[0] is not MapTerm map)
                    throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), arguments[0]));
                if (map.FlatKeys426 is not TupleTerm keys)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return ValueTask.FromResult<Term>(keys);
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
