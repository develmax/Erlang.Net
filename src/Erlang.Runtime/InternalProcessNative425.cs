namespace Erlang;

internal static class InternalProcessNative425
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
        if (owner is null || module != InternalProcessNames425.Module) return false;
        bool matches = (function, arity) is
            (InternalProcessNames425.GroupLeader, 2 or 3) or
            (InternalProcessNames425.Display, 2) or
            (InternalProcessNames425.Flag, 3) or
            (InternalProcessNames425.Suspend, 2) or
            (InternalProcessNames425.Monitor, 1 or 3);
        if (!matches) return false;
        body = async (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != arity)
                throw new InvalidOperationException(InternalProcessNames425.Foreign);
            owner.RequireInternalProcessActor425(context);
            try
            {
                return function switch
                {
                    InternalProcessNames425.GroupLeader => await owner.InternalGroupLeader425(context, arguments),
                    InternalProcessNames425.Display => owner.RequestDisplay425(context, arguments),
                    InternalProcessNames425.Flag => owner.RequestFlag425(context, arguments),
                    InternalProcessNames425.Suspend => await owner.RequestSuspend425(context, arguments),
                    InternalProcessNames425.Monitor => owner.InternalSystemMonitor425(arguments),
                    _ => throw new InvalidOperationException()
                };
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
