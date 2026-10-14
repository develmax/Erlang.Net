namespace Erlang;

public static class DistributionControllerPrimitives
{
    public static Term GetOption(ProcessContext caller, Term handle, Term option)
    {
        RequireController(caller);
        if (option is not Atom { Name: DistributionControllerNames.GetSize })
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Invoke(() => Boolean(caller.Runtime.DistributionPending.ConnectedGetSize(handle, caller)));
    }

    public static Term SetOption(
        ProcessContext caller,
        Term handle,
        Term option,
        Term value
    )
    {
        RequireController(caller);
        if (option is not Atom { Name: DistributionControllerNames.GetSize } ||
            value is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } boolean)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Invoke(() => Boolean(caller.Runtime.DistributionPending.SetConnectedGetSize(
            handle,
            caller,
            boolean.Name == ErlangBooleanAtoms.True
        )));
    }

    public static Term RequestDataNotification(ProcessContext caller, Term handle)
    {
        RequireController(caller);

        return Invoke(() =>
        {
            caller.Runtime.DistributionPending.RequestConnectedOutputNotification(handle, caller);

            return Term.A(CommonAtomNames.Ok);
        });
    }

    public static Term GetData(ProcessContext caller, Term handle)
    {
        RequireController(caller);

        return Invoke(() =>
        {
            var buffer = caller.Runtime.DistributionPending.TakeConnectedOutput(handle, caller, out bool getSize);
            if (buffer is null)
                return Term.A(DistributionControllerNames.NoData);
            Term segments = Cons.From(buffer.Segments.Cast<Term>());

            return getSize ? Term.Tuple(Term.I(buffer.PayloadBytes), segments) : segments;
        });
    }

    public static Term SetInputHandler(ProcessContext caller, Term handle, Term handler)
    {
        ArgumentNullException.ThrowIfNull(caller);
        if (!caller.Runtime.DistributionPending.HasDistributionHandle(handle))
            throw new ErlangException(DistributionControllerNames.NotSupported);
        if (caller.DistributionRole != RuntimeDistributionRole.Controller || handler is not Pid pid)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Invoke(() =>
        {
            caller.Runtime.DistributionPending.SetConnectedInputHandler(handle, caller, pid);

            return Term.A(CommonAtomNames.Ok);
        });
    }

    private static void RequireController(ProcessContext caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        if (caller.DistributionRole == RuntimeDistributionRole.None)
            throw new ErlangException(DistributionControllerNames.NotSupported);
        if (caller.DistributionRole != RuntimeDistributionRole.Controller)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    private static Term Boolean(bool value) => Term.A(value ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);

    private static Term Invoke(Func<Term> operation)
    {
        try
        {
            return operation();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }
}
