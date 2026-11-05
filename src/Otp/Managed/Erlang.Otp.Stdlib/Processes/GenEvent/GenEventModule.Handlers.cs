// Derived from Erlang/OTP 29.1.1 gen_event.erl (Apache-2.0), Ericsson AB.
namespace Erlang;

public static partial class GenEventModule
{
    private static async ValueTask<(bool Hibernate, Term Reply)> Add(
        ProcessContext context,
        List<GenEventHandler> handlers,
        Term identity,
        Term arguments,
        Term supervisor
    )
    {
        if (!Is(supervisor, ErlangBooleanAtoms.False))
        {
            await External(
                context,
                GenEventDependencyModules.Erlang,
                GenEventDependencyFunctions.Link,
                supervisor
            );
        }
        GenEventHandler handler = GenEventHandler.Create(identity, supervisor);
        using var source = GenEventSource.Frame(
            context,
            GenEventSourceFunctions.ServerAddHandler,
            GenEventSourceArities.ServerAddHandler,
            GenEventSourceLines.HandlerInitialization
        );
        Term result = await Caught(
            context,
            handler.Module,
            GenEventCallbacks.Init,
            arguments
        );
        if (result is TupleTerm tuple && tuple.Items.Count is GenEventInitializationResultLayout.NormalSize or GenEventInitializationResultLayout.HibernateSize && Is(tuple.Items[0], GenEventResults.Ok)
            && (tuple.Items.Count == GenEventInitializationResultLayout.NormalSize || Is(tuple.Items[GenEventInitializationResultLayout.HibernateIndex], GenEventActions.Hibernate)))
        {
            handlers.Insert(0, handler with { State = tuple.Items[GenEventInitializationResultLayout.StateIndex] });

            return (tuple.Items.Count == GenEventInitializationResultLayout.HibernateSize, A(GenEventResults.Ok));
        }

        return (false, result);
    }

    private static async ValueTask<Term> TerminateHandler(
        ProcessContext context,
        GenEventHandler handler,
        Term arguments,
        Term last,
        Term name,
        Term reason
    )
    {
        Term result = A(GenEventResults.Ok);
        if (handler.Module is Atom module && context.Runtime.Modules.Exports.Contains((module.Name, GenEventCallbacks.Terminate, GenEventCallbackArities.Terminate)))
        {
            using var source = GenEventSource.Frame(
                context,
                GenEventSourceFunctions.DoTerminate,
                GenEventSourceArities.DoTerminate,
                GenEventSourceLines.HandlerTermination
            );
            result = await Caught(
                context,
                handler.Module,
                GenEventCallbacks.Terminate,
                arguments,
                handler.State
            );
        }
        await Report(
            context,
            handler,
            reason,
            handler.State,
            last,
            name
        );

        return result;
    }

    private static async ValueTask Report(
        ProcessContext context,
        GenEventHandler handler,
        Term reason,
        Term state,
        Term last,
        Term name
    )
    {
        if (!Is(reason, GenEventTermination.Normal) && !Is(reason, GenEventTermination.Shutdown)
            && !(reason is TupleTerm { Items.Count: 3 } swapped && Is(swapped.Items[0], GenEventTermination.Swapped))
            && Is(
                await External(
                    context,
                    GenEventDependencyModules.Logger,
                    GenEventDependencyFunctions.Allow,
                    A(GenEventLogNames.ErrorLevel),
                    A(Name)
                ),
                ErlangBooleanAtoms.True
            ))
        {
            Term plain = reason;
            Func<Term, Term> restore = value => value;
            if (reason is TupleTerm { Items.Count: 2 } exit && Is(exit.Items[0], GenEventProtocol.Exit))
            {
                if (exit.Items[1] is TupleTerm { Items.Count: 2 } errorStack)
                {
                    plain = errorStack.Items[0];
                    restore = value => Term.Tuple(A(GenEventProtocol.Exit), Term.Tuple(value, errorStack.Items[1]));
                }
                else
                {
                    plain = exit.Items[1];
                    restore = value => Term.Tuple(A(GenEventProtocol.Exit), value);
                }
            }
            MapTerm status = (MapTerm)await External(
                context,
                GenEventDependencyModules.Gen,
                GenEventDependencyFunctions.FormatStatus,
                handler.Module,
                A(GenEventLogNames.TerminationLabel),
                Map((GenEventStatusFields.State, state), (GenEventStatusFields.Message, last), (GenEventStatusFields.Reason, plain)),
                Term.List(await External(context, GenEventDependencyModules.Erlang, GenEventDependencyFunctions.Get), state)
            );
            Term label = await External(
                context,
                GenEventDependencyModules.ProcLib,
                GenEventDependencyFunctions.GetLabel,
                context.Self
            );
            MapTerm report = Map(
                (GenEventLogFields.Label, Term.Tuple(A(Name), A(GenEventLogNames.TerminationLabel))),
                (GenEventLogFields.Handler, handler.Identity),
                (GenEventLogFields.Name, name),
                (GenEventLogFields.LastMessage, Get(status, GenEventStatusFields.Message)),
                (GenEventLogFields.State, Get(status, GenEventStatusFields.FormattedStatus, Get(status, GenEventStatusFields.State))),
                (GenEventLogFields.Reason, restore(Get(status, GenEventStatusFields.Reason))),
                (GenEventLogFields.ProcessLabel, label)
            );
            await Log(
                context,
                GenEventLogNames.ErrorLevel,
                report,
                GenEventLogNames.ErrorTag
            );
        }
        if (!Is(handler.Supervisor, ErlangBooleanAtoms.False))
        {
            await ErlangOperatorValues.TrySendAsync(context, handler.Supervisor, Term.Tuple(A(GenEventProtocol.SupervisorExit), handler.Identity, reason));
        }
    }

    private static async ValueTask Log(
        ProcessContext context,
        string level,
        MapTerm report,
        string tag
    )
    {
        Term formatter = new FunctionTerm(
            new ExternalFunctionIdentity(A(Name), A(GenEventApi.FormatLog), GenEventCallbackArities.FormatLog),
            (execution, arguments) => Invoke((ProcessContext)execution, GenEventApi.FormatLog, arguments)
        );
        Term legacy = new FunctionTerm(
            new ExternalFunctionIdentity(A(Name), A(GenEventApi.FormatLog), GenEventCallbackArities.LegacyFormatLog),
            (execution, arguments) => Invoke((ProcessContext)execution, GenEventApi.FormatLog, arguments)
        );
        Term location = Map(
            (GenEventLogFields.Mfa, Term.Tuple(
                A(Name),
                A(level == GenEventLogNames.ErrorLevel ? GenEventSourceFunctions.ReportError : GenEventSourceFunctions.ServerUpdate),
                Term.I(level == GenEventLogNames.ErrorLevel ? GenEventSourceArities.ReportError : GenEventSourceArities.ServerUpdate)
            )),
            (GenEventLogFields.Line, Term.I(level == GenEventLogNames.ErrorLevel ? GenEventSourceLines.HandlerErrorReport : GenEventSourceLines.MissingInfoWarning)),
            (GenEventLogFields.File, Term.String(GenEventSource.File))
        );
        await External(
            context,
            GenEventDependencyModules.Logger,
            GenEventDependencyFunctions.MacroLog,
            location,
            A(level),
            report,
            Map(
                (GenEventLogFields.Domain, Term.List(A(GenEventLogNames.OtpDomain))),
                (GenEventLogFields.ReportCb, formatter),
                (GenEventLogFields.ErrorLogger, Map((GenEventLogFields.Tag, A(tag)), (GenEventLogFields.ReportCb, legacy)))
            )
        );
    }

    private static int Find(List<GenEventHandler> handlers, Term identity)
    {
        GenEventHandler target = GenEventHandler.Create(identity, A(ErlangBooleanAtoms.False));

        return handlers.FindIndex(handler => handler.Module.Equals(target.Module) && handler.Id.Equals(target.Id));
    }

    private static async ValueTask<(bool Hibernate, Term Reply)> Swap(
        ProcessContext context,
        List<GenEventHandler> handlers,
        Term oldIdentity,
        Term oldArguments,
        Term newIdentity,
        Term newArguments,
        Term supervisor,
        Term name
    )
    {
        int index = Find(handlers, oldIdentity);
        Term transfer = A(GenEventResults.Error);
        Term inherited = A(ErlangBooleanAtoms.False);
        if (index >= 0)
        {
            GenEventHandler old = handlers[index];
            handlers.RemoveAt(index);
            inherited = old.Supervisor;
            transfer = await TerminateHandler(
                context,
                old,
                oldArguments,
                A(GenEventTermination.SwappedInput),
                name,
                Term.Tuple(A(GenEventTermination.Swapped), newIdentity, Is(supervisor, ErlangBooleanAtoms.False) ? inherited : supervisor)
            );
        }
        var result = await Add(
            context,
            handlers,
            newIdentity,
            Term.Tuple(newArguments, transfer),
            Is(supervisor, ErlangBooleanAtoms.False) ? inherited : supervisor
        );

        return (result.Hibernate, Is(result.Reply, GenEventResults.Ok) ? result.Reply : Error(result.Reply));
    }

    private static async ValueTask<GenEventHandler?> CallbackSwap(
        ProcessContext context,
        GenEventHandler old,
        Term oldArguments,
        Term state,
        Term identity,
        Term arguments,
        Term name
    )
    {
        Term transfer = await TerminateHandler(
            context,
            old with { State = state },
            oldArguments,
            A(GenEventTermination.SwappedInput),
            name,
            Term.Tuple(A(GenEventTermination.Swapped), identity, old.Supervisor)
        );
        GenEventHandler handler = GenEventHandler.Create(identity, old.Supervisor);
        Term result = await Caught(
            context,
            handler.Module,
            GenEventCallbacks.Init,
            Term.Tuple(arguments, transfer)
        );
        // OTP deliberately accepts only {ok, State} for a callback-requested swap.
        if (result is TupleTerm { Items.Count: GenEventInitializationResultLayout.NormalSize } ok && Is(ok.Items[0], GenEventResults.Ok))
        {
            return handler with { State = ok.Items[GenEventInitializationResultLayout.StateIndex] };
        }
        await Report(
            context,
            handler,
            A(GenEventTermination.Crash),
            Error(result),
            name,
            A(ErlangBooleanAtoms.False)
        );

        return null;
    }

    private static async ValueTask<(GenEventHandler? Handler, bool Hibernate, Term Reply)> Update(
        ProcessContext context,
        GenEventHandler handler,
        string function,
        Term input,
        Term name
    )
    {
        bool call = function == GenEventCallbacks.HandleCall;
        using var source = GenEventSource.Frame(
            context,
            call ? GenEventSourceFunctions.ServerCallUpdate : GenEventSourceFunctions.ServerUpdate,
            call ? GenEventSourceArities.ServerCallUpdate : GenEventSourceArities.ServerUpdate,
            call ? GenEventSourceLines.CallCallback : GenEventSourceLines.EventCallback
        );
        Term result = await Caught(
            context,
            handler.Module,
            function,
            input,
            handler.State
        );
        if (result is TupleTerm tuple && tuple.Items.Count > 0)
        {
            int expected = call ? GenEventCallbackResults.CallSize : GenEventCallbackResults.EventSize;
            if (Is(tuple.Items[0], GenEventResults.Ok) && (tuple.Items.Count == expected
                || tuple.Items.Count == expected + GenEventCallbackResults.HibernateExtensionSize && Is(tuple.Items[expected], GenEventActions.Hibernate)))
            {
                return (handler with { State = tuple.Items[call ? GenEventCallbackResults.CallStateIndex : GenEventCallbackResults.EventStateIndex] }, tuple.Items.Count != expected,
                    call ? tuple.Items[GenEventCallbackResults.CallReplyIndex] : A(GenEventResults.Ok));
            }
            if (tuple.Items.Count == (call ? GenEventCallbackResults.CallSwapSize : GenEventCallbackResults.EventSwapSize) && Is(tuple.Items[0], GenEventActions.SwapHandler))
            {
                GenEventHandler? replacement = await CallbackSwap(
                    context,
                    handler,
                    tuple.Items[call ? GenEventCallbackResults.CallSwapArgumentsIndex : GenEventCallbackResults.EventSwapArgumentsIndex],
                    tuple.Items[call ? GenEventCallbackResults.CallSwapStateIndex : GenEventCallbackResults.EventSwapStateIndex],
                    tuple.Items[call ? GenEventCallbackResults.CallSwapHandlerIndex : GenEventCallbackResults.EventSwapHandlerIndex],
                    tuple.Items[call ? GenEventCallbackResults.CallSwapNewArgumentsIndex : GenEventCallbackResults.EventSwapNewArgumentsIndex],
                    name
                );

                return (replacement, false, call ? tuple.Items[GenEventCallbackResults.CallReplyIndex] : A(GenEventResults.Ok));
            }
            if (call && tuple.Items.Count == GenEventCallbackResults.CallRemoveSize && Is(tuple.Items[0], GenEventActions.RemoveHandler))
            {
                await TerminateHandler(
                    context,
                    handler,
                    A(GenEventActions.RemoveHandler),
                    A(GenEventTermination.RemoveInput),
                    name,
                    A(GenEventTermination.Normal)
                );

                return (null, false, tuple.Items[GenEventCallbackResults.CallRemoveReplyIndex]);
            }
        }
        if (!call && Is(result, GenEventActions.RemoveHandler))
        {
            await TerminateHandler(
                context,
                handler,
                result,
                A(GenEventTermination.RemoveInput),
                name,
                A(GenEventTermination.Normal)
            );

            return (null, false, A(GenEventResults.Ok));
        }
        if (function == GenEventCallbacks.HandleInfo && MissingInfo(result, handler.Module))
        {
            if (Is(
                await External(
                    context,
                    GenEventDependencyModules.Logger,
                    GenEventDependencyFunctions.Allow,
                    A(GenEventLogNames.WarningLevel),
                    A(Name)
                ),
                ErlangBooleanAtoms.True
            ))
            {
                await Log(
                    context,
                    GenEventLogNames.WarningLevel,
                    Map(
                        (GenEventLogFields.Label, Term.Tuple(A(Name), A(GenEventLogNames.MissingInfoLabel))),
                        (GenEventLogFields.Module, handler.Module),
                        (GenEventLogFields.Message, input)
                    ),
                    GenEventLogNames.WarningTag
                );
            }

            return (handler, false, A(GenEventResults.Ok));
        }
        await TerminateHandler(
            context,
            handler,
            Error(result),
            input,
            name,
            result
        );

        return (null, false, Error(result));
    }

    private static bool MissingInfo(Term result, Term module)
        => result is TupleTerm { Items.Count: 2 } exit && Is(exit.Items[0], GenEventProtocol.Exit)
            && exit.Items[1] is TupleTerm { Items.Count: 2 } reason && Is(reason.Items[0], ErlangErrorReasons.UndefinedFunction)
            && reason.Items[1] is Cons { Head: TupleTerm { Items.Count: GenEventStackFrameLayout.FieldCount } frame }
            && frame.Items[GenEventStackFrameLayout.ModuleIndex].Equals(module) && Is(frame.Items[GenEventStackFrameLayout.FunctionIndex], GenEventCallbacks.HandleInfo)
            && frame.Items[GenEventStackFrameLayout.ArgumentsIndex] is Cons { Tail: Cons { Tail: Nil } };

    private static async ValueTask<bool> Notify(
        ProcessContext context,
        List<GenEventHandler> handlers,
        string function,
        Term input,
        Term name
    )
    {
        bool hibernate = false;
        var updated = new List<GenEventHandler>();
        var continuations = new List<IDisposable>();
        try
        {
            foreach (GenEventHandler handler in handlers)
            {
                await context.ReduceAsync();
                (GenEventHandler? Handler, bool Hibernate, Term Reply) result;
                using (GenEventSource.Frame(
                    context,
                    GenEventSourceFunctions.ServerNotify,
                    GenEventSourceArities.ServerNotify,
                    GenEventSourceLines.NotificationUpdate
                ))
                {
                    result = await Update(
                        context,
                        handler,
                        function,
                        input,
                        name
                    );
                }
                if (result.Handler is not null)
                {
                    updated.Add(result.Handler);
                    hibernate |= result.Hibernate;
                    continuations.Add(GenEventSource.Frame(
                        context,
                        GenEventSourceFunctions.ServerNotify,
                        GenEventSourceArities.ServerNotify,
                        result.Hibernate ? GenEventSourceLines.HibernateContinuation : GenEventSourceLines.NotificationContinuation
                    ));
                }
            }
        }
        finally
        {
            for (int index = continuations.Count - 1; index >= 0; index--)
            {
                continuations[index].Dispose();
            }
        }
        handlers.Clear();
        handlers.AddRange(updated);

        return hibernate;
    }

    private static MapTerm Map(params (string Key, Term Value)[] pairs)
        => new(pairs.Select(pair => new KeyValuePair<Term, Term>(A(pair.Key), pair.Value)));

    private static Term Get(MapTerm map, string key, Term? fallback = null)
        => map.TryGet(A(key), out Term? value) ? value! : fallback ?? throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadKey), A(key)));
}
