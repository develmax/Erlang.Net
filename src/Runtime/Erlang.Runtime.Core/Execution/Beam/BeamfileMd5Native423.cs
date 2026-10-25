namespace Erlang;

internal static class BeamfileMd5Native423
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
        if (owner is null || module != BeamfileMd5Names423.Module || function != BeamfileMd5Names423.Function || arity != BeamfileMd5Names423.Arity) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != BeamfileMd5Names423.Arity)
                throw new InvalidOperationException(BeamfileMd5Names423.Foreign);
            owner.RequireBeamfileActor418(context);
            try
            {
                if (arguments[0] is not BitString { IsBinary: true } binary)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                Term result;
                try
                {
                    result = new BeamfileMd5Document423(owner, context.Cancellation).Read(binary.ToArray());
                }
                catch (InvalidDataException)
                {
                    result = Term.A(BeamfileMd5Names423.Undefined);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                    result = Term.A(BeamfileMd5Names423.Undefined);
                }
                owner.RequireBeamfileActor418(context);

                return ValueTask.FromResult(result);
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
