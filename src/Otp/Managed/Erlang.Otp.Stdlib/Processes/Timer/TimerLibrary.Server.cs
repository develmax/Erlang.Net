// Derived from Erlang/OTP 29.1.1 timer.erl, Apache-2.0; Ericsson AB 1996-2025.
using static Erlang.TimerLibraryNames;

namespace Erlang;

public static partial class TimerLibrary
{
    private static async ValueTask<Term> Start(ProcessContext context)
    {
        var spec = new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(TimerChildSpecificationNames.Id)] = Term.A(Server),
            [Term.A(TimerChildSpecificationNames.Start)] = Term.Tuple(Term.A(Module), Term.A(TimerLibraryFunctions.StartLink), Nil.Value),
            [Term.A(TimerChildSpecificationNames.Restart)] = Term.A(TimerChildSpecificationNames.Permanent),
            [Term.A(TimerChildSpecificationNames.Shutdown)] = Term.I(TimerTimeParameters.ShutdownMilliseconds),
            [Term.A(TimerChildSpecificationNames.Type)] = Term.A(TimerChildSpecificationNames.Worker),
            [Term.A(TimerChildSpecificationNames.Modules)] = Term.List(Term.A(Module))
        });
        Term result = await Call(
            context,
            TimerDependencyNames.Supervisor,
            TimerDependencyFunctions.StartChild,
            Term.A(TimerDependencyNames.KernelSupervisor),
            spec
        );
        if (result is TupleTerm tuple)
        {
            if (tuple.Items.Count is TimerProtocolArities.Result or TimerProtocolArities.ResultWithInfo && tuple.Items[0].Equals(Ok))
            {
                return Term.Tuple(Ok, tuple.Items[1]);
            }
            if (tuple.Items.Count == TimerProtocolArities.Result && tuple.Items[0].Equals(Error))
            {
                if (tuple.Items[1] is Atom { Name: TimerChildSpecificationNames.AlreadyPresent })
                {
                    result = await Call(
                        context,
                        TimerDependencyNames.Supervisor,
                        TimerDependencyFunctions.RestartChild,
                        Term.A(TimerDependencyNames.KernelSupervisor),
                        Term.A(Server)
                    );
                    if (result is TupleTerm { Items.Count: TimerProtocolArities.Result } restarted && restarted.Items[0].Equals(Ok))
                    {
                        return result;
                    }
                    if (result is not TupleTerm { Items.Count: TimerProtocolArities.Result } error || !error.Items[0].Equals(Error)
                        || error.Items[1] is not TupleTerm { Items.Count: TimerProtocolArities.Result } started || started.Items[0] is not Atom { Name: Erlang.Otp.BehaviourAtoms.AlreadyStarted })
                    {
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), result));
                    }

                    return Term.Tuple(Ok, started.Items[1]);
                }
                if (tuple.Items[1] is TupleTerm { Items.Count: TimerProtocolArities.Result } existing && existing.Items[0] is Atom { Name: Erlang.Otp.BehaviourAtoms.AlreadyStarted })
                {
                    return Term.Tuple(Ok, existing.Items[1]);
                }
            }
        }

        return result;
    }

    private static async ValueTask<Term> Request(ProcessContext context, string request, Term argument)
    {
        try
        {
            return await MaybeRequest(context, request, argument);
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit
            && error.Reason is TupleTerm { Items.Count: TimerProtocolArities.Result } reason && reason.Items[0] is Atom { Name: ErlangErrorReasons.NoProcess })
        {
            RequireOk(await Start(context));

            return await MaybeRequest(context, request, argument);
        }
    }

    private static ValueTask<Term> MaybeRequest(ProcessContext context, string request, Term argument)
        => Call(
            context,
            TimerDependencyNames.GenServer,
            TimerDependencyFunctions.ServerCall,
            Term.A(Server),
            Term.Tuple(Term.A(request), argument),
            Term.A(Erlang.Otp.BehaviourAtoms.Infinity)
        );

    private static async ValueTask<Term> Callback(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        Term state = arguments[^1];
        switch (function)
        {
            case TimerLibraryFunctions.Initialize:
                if (arguments[0] is not Nil)
                {
                    throw TimerSource.Clause(
                        context,
                        function,
                        arguments,
                        TimerSourceLines.InitializeClause
                    );
                }
                _ = await Bif(
                    context,
                    ErlangModuleNames.ProcessFlag,
                    Term.A(TimerProtocolAtoms.TrapExit),
                    Term.A(ErlangBooleanAtoms.True)
                );

                return Term.Tuple(
                    Ok,
                    await Call(
                        context,
                        TimerDependencyNames.Ets,
                        TimerDependencyFunctions.NewTable,
                        Term.A(Module),
                        Term.List(Term.A(TimerProtocolAtoms.PrivateTable))
                    )
                );
            case TimerLibraryFunctions.CodeChange:
                return Term.Tuple(Ok, arguments[1]);
            case TimerLibraryFunctions.HandleCast:
                return Term.Tuple(Term.A(Erlang.Otp.BehaviourAtoms.NoReply), state);
            case TimerLibraryFunctions.HandleCall:
                return await HandleCall(context, arguments[0], state);
            case TimerLibraryFunctions.HandleInfo:
                {
                    if (arguments[0] is TupleTerm message)
                    {
                        if (message.Items.Count == TimerProtocolArities.TimeoutMessage && message.Items[0] is Atom { Name: TimerProtocolAtoms.Timeout }
                            && message.Items[2] is TupleTerm { Items.Count: TimerProtocolArities.ServerRequest } payload && payload.Items[0] is Atom { Name: TimerRequestNames.ApplyOnce })
                        {
                            Term taken = await Call(
                                context,
                                TimerDependencyNames.Ets,
                                TimerDependencyFunctions.Take,
                                state,
                                message.Items[1]
                            );
                            if (taken is Cons { Head: TupleTerm { Items.Count: TimerProtocolArities.OnceEntry } entry, Tail: Nil }
                                && entry.Items[0].Equals(message.Items[1]))
                            {
                                _ = await DoApply(context, payload.Items[1], false);
                            }
                            else if (taken is not Nil)
                            {
                                throw TimerSource.Error(
                                    context,
                                    TimerLibraryFunctions.HandleInfo,
                                    2,
                                    TimerSourceLines.HandleTimeoutTake,
                                    Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), taken)
                                );
                            }
                        }
                        else if (message.Items.Count == TimerProtocolArities.DownMessage && message.Items[0] is Atom { Name: TimerProtocolAtoms.Down }
                            && message.Items[2] is Atom { Name: TimerProtocolAtoms.Process })
                        {
                            _ = await Remove(context, message.Items[1], state);
                        }
                    }

                    return Term.Tuple(Term.A(Erlang.Otp.BehaviourAtoms.NoReply), state);
                }
            case TimerLibraryFunctions.Terminate:
                if (state is not Atom { Name: TimerProtocolAtoms.Undefined })
                {
                    FunctionTerm fold = new TimerTerminationRecipe(context.Runtime.Functions).Rebind(Array.Empty<Term>());
                    _ = await TimerSource.Run(
                        context,
                        TimerLibraryFunctions.Terminate,
                        2,
                        TimerSourceLines.TerminateFold,
                        () =>
                        Call(
                            context,
                            TimerDependencyNames.Ets,
                            TimerDependencyFunctions.FoldLeft,
                            fold,
                            Term.A(TimerProtocolAtoms.Undefined),
                            state
                        )
                    );
                    _ = await Call(
                        context,
                        TimerDependencyNames.Ets,
                        TimerDependencyFunctions.DeleteTable,
                        state
                    );
                }

                return Ok;
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    private static async ValueTask<Term> HandleCall(ProcessContext context, Term request, Term state)
    {
        if (request is TupleTerm { Items.Count: TimerProtocolArities.ServerRequest } pair)
        {
            if (pair.Items[0] is Atom { Name: TimerRequestNames.ApplyOnce } && pair.Items[1] is TupleTerm { Items.Count: TimerProtocolArities.OnceRequest } once)
            {
                Term timeout = CoreModules.Arithmetic(ErlangOperators.Plus, once.Items[0], once.Items[1]);
                Term reply;
                try
                {
                    Term reference = await Bif(
                        context,
                        TimerModuleNames.StartTimer,
                        timeout,
                        context.Self,
                        Term.Tuple(Term.A(TimerRequestNames.ApplyOnce), once.Items[2]),
                        Absolute
                    );
                    _ = await Call(
                        context,
                        TimerDependencyNames.Ets,
                        TimerDependencyFunctions.Insert,
                        state,
                        Term.Tuple(reference)
                    );
                    reply = Term.Tuple(Ok, Term.Tuple(Term.A(TimerReferenceKinds.Once), reference));
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason is Atom { Name: ErlangErrorReasons.BadArgument })
                {
                    reply = BadArgument;
                }

                return Term.Tuple(Term.A(Erlang.Otp.BehaviourAtoms.Reply), reply, state);
            }
            if (pair.Items[0] is Atom { Name: TimerRequestNames.ApplyInterval or TimerRequestNames.ApplyRepeatedly } operation
                && pair.Items[1] is TupleTerm { Items.Count: TimerProtocolArities.IntervalRequest } interval)
            {
                Term entry = await StartInterval(context, interval.Items, operation.Name == TimerRequestNames.ApplyRepeatedly);
                _ = await Call(
                    context,
                    TimerDependencyNames.Ets,
                    TimerDependencyFunctions.Insert,
                    state,
                    entry
                );

                return Term.Tuple(
                    Term.A(Erlang.Otp.BehaviourAtoms.Reply),
                    Term.Tuple(Ok, Term.Tuple(Term.A(TimerReferenceKinds.Interval), ((TupleTerm)entry).Items[0])),
                    state
                );
            }
            if (pair.Items[0] is Atom { Name: TimerRequestNames.Cancel }
                && pair.Items[1] is TupleTerm { Items.Count: TimerProtocolArities.Reference } cancel && cancel.Items[0] is Atom { Name: TimerReferenceKinds.Once or TimerReferenceKinds.Interval } type)
            {
                bool removed = await Remove(context, cancel.Items[1], state);
                if (removed && type.Name == TimerReferenceKinds.Interval)
                {
                    _ = await Bif(
                        context,
                        ErlangModuleNames.Demonitor,
                        cancel.Items[1],
                        Term.List(Term.A(TimerProtocolAtoms.Flush))
                    );
                }

                return Term.Tuple(Term.A(Erlang.Otp.BehaviourAtoms.Reply), Cancelled, state);
            }
        }

        return Term.Tuple(Term.A(Erlang.Otp.BehaviourAtoms.NoReply), state);
    }

    private static async ValueTask<bool> Remove(ProcessContext context, Term reference, Term state)
    {
        Term taken = await Call(
            context,
            TimerDependencyNames.Ets,
            TimerDependencyFunctions.Take,
            state,
            reference
        );
        if (taken is Cons { Tail: Nil, Head: TupleTerm entry } && entry.Items.Count is TimerProtocolArities.OnceEntry or TimerProtocolArities.IntervalEntry && entry.Items[0].Equals(reference))
        {
            if (entry.Items.Count == TimerProtocolArities.OnceEntry)
            {
                _ = await CancelTimer(context, reference);
            }
            else if (entry.Items.Count == TimerProtocolArities.IntervalEntry)
            {
                _ = await Bif(
                    context,
                    ErlangBasicBifNames.Send,
                    entry.Items[1],
                    Term.Tuple(Term.A(TimerProtocolAtoms.Cancel), entry.Items[2])
                );
            }
            else
            {
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), taken));
            }

            return true;
        }
        if (taken is not Nil)
        {
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), taken));
        }

        return false;
    }

    private static ValueTask<Term> CancelTimer(ProcessContext context, Term reference)
        => Bif(
            context,
            TimerModuleNames.CancelTimer,
            reference,
            CancelOptions
        );

    internal static async ValueTask<Term> TerminateEntry(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is TupleTerm { Items.Count: TimerProtocolArities.OnceEntry } once)
        {
            _ = await CancelTimer(context, once.Items[0]);
        }
        else if (arguments[0] is TupleTerm { Items.Count: TimerProtocolArities.IntervalEntry } interval)
        {
            _ = await Bif(
                context,
                ErlangBasicBifNames.Send,
                interval.Items[1],
                Term.Tuple(Term.A(TimerProtocolAtoms.Cancel), interval.Items[2])
            );
        }
        else
        {
            throw TimerSource.Clause(
                context,
                TimerClosureNames.Termination,
                arguments,
                TimerSourceLines.TerminateEntryClause
            );
        }

        return arguments[1];
    }
}
