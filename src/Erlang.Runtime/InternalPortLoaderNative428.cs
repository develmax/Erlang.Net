namespace Erlang;

internal static class InternalPortLoaderNative428
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
        if (owner is null || module != InternalPortLoaderNames428.Module
            || (function, arity) is not ((InternalPortLoaderNames428.Call, 3) or (InternalPortLoaderNames428.Control, 3) or (InternalPortLoaderNames428.Prepare, 2)))
            return false;
        body = async (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != arity)
                throw new InvalidOperationException(InternalPortLoaderNames428.Foreign);
            owner.RequireInternalPortActor416(context);
            if (function == InternalPortLoaderNames428.Prepare)
            {
                try
                {
                    return BeamLoaderApi.PrepareImage(context, arguments[0], arguments[1]);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && !error.ExplicitStackTrace)
                {
                    throw context.CaptureNativeFailure(
                        error,
                        module,
                        function,
                        arguments
                    );
                }
            }
            Term result;
            try
            {
                // A foreign CLR-owned port is not an authentic handle in this VM,
                // even if its wire node/id happens to collide with a local port.
                owner.RequireInternalPortIdentity428(arguments[0]);
                result = function == InternalPortLoaderNames428.Call
                    ? await owner.DriverCall422(
                        context,
                        arguments[0],
                        arguments[1],
                        arguments[2],
                        internalProtocol428: true
                    )
                    : await owner.DriverControl422(
                        context,
                        arguments[0],
                        arguments[1],
                        arguments[2],
                        internalProtocol428: true
                    );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && !error.ExplicitStackTrace && error.Reason is Atom { Name: ErlangErrorReasons.BadArgument })
            {
                result = error.Reason;
            }
            // The reference path carries its source order in the actual port reply.
            // Immediate native status returns must admit the real incoming prefix.
            if (result is not ReferenceTerm) owner.AdmitSignals(context);

            return result;
        };

        return true;
    }
}
