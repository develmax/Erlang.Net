// Modified: OTP-29.1.1 gen_server startup, action dispatch and stop/termination scheduling.
namespace Erlang.Otp;

public static class GenServer
{
    public static async ValueTask<ProcessHandle> Start(
        ProcessRuntime runtime,
        IGenServer callback,
        Term arguments,
        ProcessContext? parent = null,
        bool link = false
    )
    {
        var result = await StartCore(
            runtime,
            callback,
            arguments,
            parent,
            link,
            null
        );
        if (result.Outcome is not TupleTerm { Items.Count: 2 } tuple || tuple.Items[0] is not Atom { Name: BehaviourAtoms.Ok })
            throw new ErlangException(result.Outcome is TupleTerm error ? error.Items[1] : result.Outcome);

        return result.Process;
    }

    public static async ValueTask<Term> StartModule(
        ProcessContext parent,
        string module,
        Term arguments,
        bool link = false,
        string? name = null
    ) => (await StartCore(
        parent.Runtime,
        new ErlangGenServer(module),
        arguments,
        parent,
        link,
        name
    )).Outcome;

    private static async ValueTask<(ProcessHandle Process, Term Outcome)> StartCore(
        ProcessRuntime runtime,
        IGenServer callback,
        Term arguments,
        ProcessContext? parent,
        bool link,
        string? name
    )
    {
        var ready = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async context =>
        {
            Term? state = null;
            Term reason = Term.A(ProcessExitReasons.Normal);
            bool terminated = false;

            async ValueTask Terminate()
            {
                if (!terminated && state is not null && context.ExitReason is null)
                {
                    terminated = true;
                    await callback.Terminate(context, reason, state);
                }
            }
            try
            {
                if (name is not null)
                {
                    if (runtime.WhereIs(name) is Pid existing)
                    {
                        ready.TrySetResult(Term.Tuple(Term.A(BehaviourAtoms.Error), Term.Tuple(Term.A(BehaviourAtoms.AlreadyStarted), existing)));

                        return Term.A(BehaviourAtoms.Ok);
                    }
                    runtime.Register(name, context.Self);
                }
                var initial = await callback.Initialize(context, arguments);
                if (initial.Ignore || initial.Error is not null)
                {
                    ready.TrySetResult(initial.Ignore ? Term.A(BehaviourAtoms.Ignore) : Term.Tuple(Term.A(BehaviourAtoms.Error), initial.Error!));
                    if (initial.Error is not null)
                        context.Exit(initial.StartupExitReason ?? initial.Error);

                    return Term.A(BehaviourAtoms.Ok);
                }
                state = initial.State!;
                if (link && parent is not null)
                    runtime.Link(context, parent.Self);
                ready.TrySetResult(Term.Tuple(Term.A(BehaviourAtoms.Ok), context.Self));
                TimeSpan? timeout = initial.Timeout;
                Term? continuation = initial.Continue;
                while (true)
                {
                    await context.ReduceAsync();
                    ServerResult result;
                    ServerFrom? from = null;
                    if (continuation is not null)
                        result = await callback.HandleContinue(context, continuation, state);
                    else
                    {
                        var message = await context.ReceiveAsync(t => t, timeout) ?? Term.A(BehaviourAtoms.Timeout);
                        if (message is TupleTerm { Items.Count: 3 } system && system.Items[0] is Atom { Name: BehaviourAtoms.System } && system.Items[2] is TupleTerm { Items.Count: 2 } stop && stop.Items[0] is Atom { Name: BehaviourAtoms.Terminate })
                        {
                            reason = stop.Items[1];
                            await Terminate();
                            context.Exit(reason);
                        }
                        if (link && parent is not null && message is TupleTerm { Items.Count: 3 } exit && exit.Items[0].Equals(Term.A(ProcessMessageTags.LinkedExit)) && exit.Items[1].Equals(parent.Self))
                        {
                            reason = exit.Items[2];
                            await Terminate();
                            context.Exit(reason);
                        }
                        if (message is TupleTerm { Items.Count: 3 } call && call.Items[0].Equals(Term.A(GenServerMessageTags.Call)) && call.Items[1] is TupleTerm { Items.Count: 2 } sender && sender.Items[0] is Pid pid && sender.Items[1] is ReferenceTerm reference)
                        {
                            from = new(pid, reference);
                            result = await callback.HandleCall(
                                context,
                                call.Items[2],
                                from,
                                state
                            );
                        }
                        else if (message is TupleTerm { Items.Count: 2 } cast && cast.Items[0].Equals(Term.A(GenServerMessageTags.Cast)))
                            result = await callback.HandleCast(context, cast.Items[1], state);
                        else
                            result = await callback.HandleInfo(context, message, state);
                    }
                    state = result.State;
                    timeout = result.Timeout;
                    continuation = result.Continue;
                    if (result.StopReason is not null)
                    {
                        reason = result.StopReason;
                        try
                        {
                            await Terminate();
                        }
                        finally
                        {
                            if (from is not null && result.Reply is not null) Reply(context, from, result.Reply);
                        }
                        context.Exit(reason);
                    }
                    if (from is not null && result.Reply is not null)
                        Reply(context, from, result.Reply);
                }
            }
            catch (ErlangException ex)
            {
                reason = callback is not ErlangGenServer || ex.ExceptionClass == ErlangExceptionClasses.Exit ? ex.Reason : Term.Tuple(
                    ex.ExceptionClass == ErlangExceptionClasses.Throw ? Term.Tuple(Term.A(BehaviourAtoms.NoCatch), ex.Reason) : ex.Reason,
                    ex.StackTraceTerm
                );
                ready.TrySetResult(Term.Tuple(Term.A(BehaviourAtoms.Error), reason));
                throw;
            }
            catch (Exception ex)
            {
                ready.TrySetException(ex);
                throw;
            }
            finally
            {
                await Terminate();
            }
        });
        Term outcome = await ready.Task;
        if (outcome is not TupleTerm { Items.Count: 2 } success || success.Items[0] is not Atom { Name: BehaviourAtoms.Ok })
            await process.Completion;

        return (process, outcome);
    }

    public static void Cast(ProcessContext context, Pid server, Term request) => context.Runtime.Send(server, Term.Tuple(Term.A(GenServerMessageTags.Cast), request));

    public static void Reply(ProcessContext context, ServerFrom from, Term reply) => context.Runtime.Send(from.Pid, Term.Tuple(from.Reference, reply));

    public static async ValueTask<Term> Call(
        ProcessContext context,
        Pid server,
        Term request,
        TimeSpan? timeout = null,
        bool infinite = false
    )
    {
        if (server.Equals(context.Self))
            throw new ErlangException(Term.A(BehaviourAtoms.CallingSelf), ErlangExceptionClasses.Exit);
        var reference = context.Runtime.Monitor(context, server);
        try
        {
            context.Runtime.Send(server, Term.Tuple(Term.A(GenServerMessageTags.Call), Term.Tuple(context.Self, reference), request));
            var response = await context.ReceiveAsync(
                t => t is TupleTerm tuple && ((tuple.Items.Count == 2 && tuple.Items[0].Equals(reference)) || (tuple.Items.Count == 5 && tuple.Items[0].Equals(Term.A(ProcessMessageTags.MonitorDown)) && tuple.Items[1].Equals(reference))) ? tuple : null,
                timeout
            );
            if (response is null)
                throw new ErlangException(Term.A(ErlangErrorReasons.Timeout), ErlangExceptionClasses.Exit);
            if (response.Items.Count == 5)
                throw new ErlangException(response.Items[4], ErlangExceptionClasses.Exit);

            return response.Items[1];
        }
        finally
        {
            context.Runtime.Demonitor(context, reference, true);
        }
    }

    public static async ValueTask<Term> Stop(
        ProcessContext context,
        Pid server,
        Term reason,
        TimeSpan? timeout = null
    )
    {
        if (server.Equals(context.Self))
            throw new ErlangException(Term.A(BehaviourAtoms.CallingSelf), ErlangExceptionClasses.Exit);
        var reference = context.Runtime.Monitor(context, server);
        try
        {
            context.Runtime.Send(
                server,
                Term.Tuple(Term.A(BehaviourAtoms.System), Term.Tuple(context.Self, reference), Term.Tuple(Term.A(BehaviourAtoms.Terminate), reason))
            );
            var down = await context.ReceiveAsync(
                t => t is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(Term.A(ProcessMessageTags.MonitorDown)) && tuple.Items[1].Equals(reference) ? tuple : null,
                timeout
            );
            if (down is null)
                throw new ErlangException(Term.A(ErlangErrorReasons.Timeout), ErlangExceptionClasses.Exit);
            if (!down.Items[4].Equals(reason))
                throw new ErlangException(down.Items[4], ErlangExceptionClasses.Exit);

            return Term.A(BehaviourAtoms.Ok);
        }
        finally
        {
            context.Runtime.Demonitor(context, reference, true);
        }
    }
}
