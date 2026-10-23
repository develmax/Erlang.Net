namespace Erlang;

public static class DistributionControllerAdapters
{
    public static async ValueTask<Term> SetNode(
        ProcessContext requester,
        Term node,
        Term controller,
        Term options
    )
    {
        try
        {
            return await requester.Runtime.DistributionPending.AdoptNodeControllerAsync(
                requester,
                node,
                controller,
                options
            );
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    public static Term GetOption(ProcessContext caller, Term handle, Term option)
    {
        RequireController(caller);

        return DistributionControllerPrimitives.GetOption(caller, Resolve(caller, handle), option);
    }

    public static Term SetOption(
        ProcessContext caller,
        Term handle,
        Term option,
        Term value
    )
    {
        RequireController(caller);

        return DistributionControllerPrimitives.SetOption(
            caller,
            Resolve(caller, handle),
            option,
            value
        );
    }

    public static Term RequestDataNotification(ProcessContext caller, Term handle)
    {
        RequireController(caller);

        return DistributionControllerPrimitives.RequestDataNotification(caller, Resolve(caller, handle));
    }

    public static Term GetData(ProcessContext caller, Term handle)
    {
        RequireController(caller);

        return DistributionControllerPrimitives.GetData(caller, Resolve(caller, handle));
    }

    public static Term SetInputHandler(ProcessContext caller, Term handle, Term handler)
    {
        if (!caller.Runtime.DistributionPending.HasConnectedControllerHandle(handle))
            throw new ErlangException(DistributionControllerNames.NotSupported);
        if (caller.DistributionRole != RuntimeDistributionRole.Controller)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return DistributionControllerPrimitives.SetInputHandler(caller, Resolve(caller, handle), handler);
    }

    private static TupleTerm Resolve(ProcessContext caller, Term handle)
    {
        try
        {
            return caller.Runtime.DistributionPending.ResolveConnectedControllerHandle(handle);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    private static void RequireController(ProcessContext caller)
    {
        if (caller.DistributionRole == RuntimeDistributionRole.None)
            throw new ErlangException(DistributionControllerNames.NotSupported);
        if (caller.DistributionRole != RuntimeDistributionRole.Controller)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
    }
}
