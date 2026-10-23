namespace Erlang;

internal static class OsNative
{
    private static bool Signature(string name, int arity) => (name, arity) switch
    {
        (OsNativeNames.Env, 0) or (OsNativeNames.GetEnv, 1) or (OsNativeNames.PutEnv, 2)
        or (OsNativeNames.UnsetEnv, 1) or (OsNativeNames.GetPid, 0) or (OsNativeNames.PerfCounter, 0)
        or (OsNativeNames.SystemTime, 0) or (OsNativeNames.SystemTime, 1) or (OsNativeNames.Timestamp, 0)
        or (OsNativeNames.SetSignal, 2) => true,
        _ => false
    };

    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (OperatingSystem.IsWindows() && owner is not null && owner.OsEnvironment is not null && module == OsNativeNames.Module && Signature(function, arity))
        {
            body = (context, arguments) =>
            {
                if (!ReferenceEquals(context.Runtime, owner)) throw new InvalidOperationException(OsNativeNames.ForeignRuntime);
                context.Cancellation.ThrowIfCancellationRequested();
                RuntimeOsEnvironment environment = owner.RequireOsEnvironment();

                return ValueTask.FromResult(Invoke(
                    context,
                    environment,
                    function,
                    arguments
                ));
            };

            return true;
        }
        body = null!;

        return false;
    }

    private static Term Invoke(
        ProcessContext context,
        RuntimeOsEnvironment environment,
        string name,
        IReadOnlyList<Term> arguments
    )
        => (name, arguments.Count) switch
        {
            (OsNativeNames.Env, 0) => environment.All(context.Cancellation),
            (OsNativeNames.GetEnv, 1) => environment.Get(arguments[0], context.Cancellation),
            (OsNativeNames.PutEnv, 2) => environment.Put(arguments[0], arguments[1], context.Cancellation),
            (OsNativeNames.UnsetEnv, 1) => environment.Unset(arguments[0], context.Cancellation),
            (OsNativeNames.GetPid, 0) => OsClockNativeValues.GetPid(),
            (OsNativeNames.PerfCounter, 0) => OsClockNativeValues.PerformanceCounter(),
            (OsNativeNames.SystemTime, 0) => OsClockNativeValues.SystemTime(),
            (OsNativeNames.SystemTime, 1) => OsClockNativeValues.SystemTime(arguments[0]),
            (OsNativeNames.Timestamp, 0) => OsClockNativeValues.Timestamp(),
            (OsNativeNames.SetSignal, 2) => SetSignalWindows(context, arguments),
            _ => throw new InvalidOperationException(OsNativeNames.UnadmittedSignature)
        };

    private static Term SetSignalWindows(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[1] is not Atom { Name: OsNativeNames.Ignore or OsNativeNames.Default or OsNativeNames.Handle })
        {
            var info = new MapTerm([
                new(Term.A(RuntimeExceptionNames.Module),Term.A(OsNativeNames.Formatter)),
                new(Term.A(OsNativeNames.Cause),Term.A(OsNativeNames.BadOption))
            ]);
            throw context.CaptureNativeFailureWithInfo(
                new ErlangException(ErlangErrorReasons.BadArgument),
                OsNativeNames.Module,
                OsNativeNames.SetSignal,
                arguments,
                info
            );
        }
        // Pinned Windows erts_set_signal always returns zero. This is genuine
        // platform behaviour; Linux never captures this complete native set.
        throw new ErlangException(ErlangErrorReasons.BadArgument);
    }
}
