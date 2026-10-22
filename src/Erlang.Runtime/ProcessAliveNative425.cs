namespace Erlang;

internal static class ProcessAliveNative425
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
        if (owner is null || module != ProcessAliveNames425.Module || function != ProcessAliveNames425.Function || arity != ProcessAliveNames425.Arity) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != ProcessAliveNames425.Arity)
                throw new InvalidOperationException(ProcessAliveNames425.Foreign);
            try
            {
                return ValueTask.FromResult(owner.RequestProcessAlive425(context, arguments));
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
