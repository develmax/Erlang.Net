namespace Erlang;

internal static class DirtyQueryNative423
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
        if (owner is null || module != DirtyQueryNames423.Module || function != DirtyQueryNames423.Function || arity != DirtyQueryNames423.Arity) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != DirtyQueryNames423.Arity)
                throw new ArgumentException(DirtyQueryNames423.Foreign);
            try
            {
                return new ValueTask<Term>(owner.ReadDirtyExecution423(context, arguments[0]) ? Term.A(DirtyQueryNames423.True) : Term.A(DirtyQueryNames423.False));
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
