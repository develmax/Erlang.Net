namespace Erlang.Otp;

internal static class ProcLibStop
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 1, 3 })
            registry.Register(
                ProcLibNames.Module,
                ProcLibStopNames.Stop,
                arity,
                Stop
            );
    }

    private static async ValueTask<Term> Stop(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term process = arguments[0];
        Term reason = arguments.Count == 1 ? ProcLibStopAtoms.Normal : arguments[1];
        Term timeout = arguments.Count == 1 ? ProcLibStartupAtoms.Infinity : arguments[2];
        var monitor = (ReferenceTerm)await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ErlangModuleNames.Monitor,
            ProcLibStartupAtoms.Process,
            process
        );
        var started = (Integer)await Clock(context, ProcLibStopAtoms.Millisecond);
        try
        {
            Term result = await context.Runtime.Modules.Call(
                context,
                ProcLibStopNames.Sys,
                ProcLibStopNames.Terminate,
                process,
                reason,
                timeout
            );
            if (!result.Equals(ProcLibAtoms.Ok))
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.TryClause), result));
        }
        catch (ErlangException failure) when (failure.ExceptionClass == ErlangExceptionClasses.Exit)
        {
            if (failure.Reason is TupleTerm { Items: [var stoppedReason, TupleTerm { Items: [Atom { Name: ProcLibStopNames.Sys }, Atom { Name: ProcLibStopNames.Terminate }, _] }] })
            {
                if (stoppedReason.Equals(ProcLibStopAtoms.NoProcess) || stoppedReason.Equals(ProcLibStopAtoms.Timeout))
                {
                    context.Runtime.Demonitor(context, monitor, flush: true);
                    context.Exit(stoppedReason);
                }
                if (!stoppedReason.Equals(reason))
                {
                    context.Runtime.Demonitor(context, monitor, flush: true);
                    throw;
                }
            }
            else
            {
                context.Runtime.Demonitor(context, monitor, flush: true);
                throw;
            }
        }
        Term remaining = timeout;
        if (!timeout.Equals(ProcLibStartupAtoms.Infinity))
        {
            var now = (Integer)await Clock(context, ProcLibStopAtoms.Microsecond);
            if (timeout is not Integer milliseconds)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var rounded = (now.Value + ProcLibStopParameters.MicrosecondsPerMillisecond - 1) / ProcLibStopParameters.MicrosecondsPerMillisecond;
            remaining = new Integer(milliseconds.Value - (rounded - started.Value));
        }
        TupleTerm? down = await context.ReceiveErlangAsync(
            value => value is TupleTerm { Items: [var tag, var reference, _, _, _] } tuple &&
                tag.Equals(ProcLibStartupAtoms.Down) && reference.Equals(monitor) ? tuple : null,
            remaining
        );
        if (down is null)
        {
            context.Runtime.Demonitor(context, monitor, flush: true);
            context.Exit(ProcLibStopAtoms.Timeout);
        }
        if (!down!.Items[^1].Equals(reason))
            context.Exit(down.Items[^1]);

        return ProcLibAtoms.Ok;
    }

    private static ValueTask<Term> Clock(ProcessContext context, Atom unit)
        => context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcLibStopNames.MonotonicTime,
            unit
        );
}
