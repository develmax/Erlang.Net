// Derived from Erlang/OTP 29.1.1 gen_event.erl (Apache-2.0), Ericsson AB.
namespace Erlang;

public static partial class GenEventModule
{
    private static async ValueTask<Term> Loop(
        ProcessContext context,
        Term parent,
        Term name,
        List<GenEventHandler> handlers,
        Term timeout,
        Term debug,
        bool hibernate
    )
    {
        if (hibernate)
        {
            return await External(
                context,
                GenEventDependencyModules.ProcLib,
                GenEventDependencyFunctions.Hibernate,
                A(Name),
                A(GenEventApi.WakeHib),
                Term.List(
                    parent,
                    name,
                    Cons.From(handlers.Select(handler => handler.Encode())),
                    timeout,
                    debug
                )
            );
        }

        return await Fetch(
            context,
            parent,
            name,
            handlers,
            timeout,
            debug,
            false
        );
    }

    private static async ValueTask<Term> Fetch(
        ProcessContext context,
        Term parent,
        Term name,
        List<GenEventHandler> handlers,
        Term timeout,
        Term debug,
        bool hibernate
    )
    {
        while (true)
        {
            await context.ReduceAsync();
            TimeSpan? wait = Is(timeout, GenEventProtocol.Infinity) ? null
                : timeout is Integer integer && integer.Value >= 0 && integer.Value <= GenEventReceiveLimits.MaximumTimeoutMilliseconds
                    ? TimeSpan.FromMilliseconds((double)integer.Value) : throw new ErlangException(ErlangErrorReasons.TimeoutValue);
            Term? message = await context.ReceiveAsync(term => term, wait);
            if (message is null)
            {
                return await Loop(
                    context,
                    parent,
                    name,
                    handlers,
                    timeout,
                    debug,
                    true
                );
            }
            if (message is TupleTerm { Items.Count: GenEventMessageLayout.SystemSize } system && Is(system.Items[GenEventMessageLayout.TagIndex], GenEventRequests.System))
            {
                return await External(
                    context,
                    GenEventDependencyModules.Sys,
                    GenEventDependencyFunctions.HandleSystemMsg,
                    system.Items[GenEventMessageLayout.SystemRequestIndex],
                    system.Items[GenEventMessageLayout.SystemFromIndex],
                    parent,
                    A(Name),
                    debug,
                    State(
                        name,
                        handlers,
                        timeout,
                        hibernate
                    ),
                    A(hibernate ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
                );
            }
            if (message is TupleTerm { Items.Count: GenEventMessageLayout.ExitSize } exit && Is(exit.Items[GenEventMessageLayout.TagIndex], GenEventProtocol.Exit) && exit.Items[GenEventMessageLayout.ExitSenderIndex].Equals(parent))
            {
                return await TerminateServer(
                    context,
                    exit.Items[GenEventMessageLayout.ExitReasonIndex],
                    parent,
                    handlers,
                    name
                );
            }
            if (debug is not Nil)
            {
                var printer = new FunctionTerm(
                    GenEventProtocol.DebugPrinterArity,
                    async (execution, args) =>
                {
                    await PrintEvent(
                        (ProcessContext)execution,
                        args[0],
                        args[1],
                        args[2]
                    );

                    return A(GenEventResults.Ok);
                }
                );
                debug = await External(
                    context,
                    GenEventDependencyModules.Sys,
                    GenEventDependencyFunctions.HandleDebug,
                    debug,
                    printer,
                    name,
                    Term.Tuple(A(GenEventProtocol.IncomingDebug), message)
                );
            }
            using var handleSource = GenEventSource.Frame(
                context,
                GenEventSourceFunctions.HandleMsg,
                GenEventSourceArities.HandleMsg,
                MessageLine(message)
            );
            hibernate = false;
            if (message is TupleTerm { Items.Count: GenEventMessageLayout.NotifySize } notification && Is(notification.Items[GenEventMessageLayout.TagIndex], GenEventRequests.Notify))
            {
                hibernate = await Notify(
                    context,
                    handlers,
                    GenEventCallbacks.HandleEvent,
                    notification.Items[GenEventMessageLayout.NotifyEventIndex],
                    name
                );
            }
            else if (message is TupleTerm { Items.Count: GenEventMessageLayout.ExitSize } linked && Is(linked.Items[GenEventMessageLayout.TagIndex], GenEventProtocol.Exit))
            {
                foreach (GenEventHandler handler in handlers.ToArray())
                {
                    if (handler.Supervisor.Equals(linked.Items[GenEventMessageLayout.ExitSenderIndex]))
                    {
                        await TerminateHandler(
                            context,
                            handler,
                            Term.Tuple(A(GenEventTermination.StopInput), linked.Items[GenEventMessageLayout.ExitReasonIndex]),
                            Term.Tuple(
                                A(GenEventTermination.ParentTerminated),
                                Term.Tuple(linked.Items[GenEventMessageLayout.ExitSenderIndex], linked.Items[GenEventMessageLayout.ExitReasonIndex])
                            ),
                            name,
                            A(GenEventTermination.Shutdown)
                        );
                        handlers.Remove(handler);
                    }
                }
                await Notify(
                    context,
                    handlers,
                    GenEventCallbacks.HandleInfo,
                    message,
                    name
                );
            }
            else if (message is TupleTerm { Items.Count: GenEventMessageLayout.RequestSize } request && IsCommand(request.Items[GenEventMessageLayout.RequestCommandIndex]))
            {
                Term reply;
                Term command = request.Items[GenEventMessageLayout.RequestCommandIndex];
                if (Is(command, GenEventRequests.WhichHandlers))
                {
                    reply = Cons.From(handlers.Select(handler => handler.Identity));
                }
                else if (Is(command, GenEventRequests.GetModules))
                {
                    reply = await External(
                        context,
                        GenEventDependencyModules.Ordsets,
                        GenEventDependencyFunctions.FromList,
                        Cons.From(handlers.Select(handler => handler.Module))
                    );
                }
                else if (Is(command, GenEventRequests.Stop))
                {
                    try
                    {
                        await TerminateServer(
                            context,
                            A(GenEventTermination.Normal),
                            parent,
                            handlers,
                            name
                        );
                    }
                    catch (ErlangException)
                    {
                        // The legacy stop protocol catches exit before replying and returning.
                    }
                    await External(
                        context,
                        GenEventDependencyModules.Gen,
                        GenEventDependencyFunctions.Reply,
                        request.Items[GenEventMessageLayout.RequestReplyTagIndex],
                        A(GenEventResults.Ok)
                    );

                    return A(GenEventResults.Ok);
                }
                else
                {
                    var tuple = (TupleTerm)command;
                    var a = tuple.Items;
                    string operation = ((Atom)a[GenEventRequestLayout.OperationIndex]).Name;
                    switch (operation)
                    {
                        case GenEventRequests.SyncNotify:
                            hibernate = await Notify(
                                context,
                                handlers,
                                GenEventCallbacks.HandleEvent,
                                a[GenEventRequestLayout.HandlerIndex],
                                name
                            );
                            reply = A(GenEventResults.Ok);
                            break;
                        case GenEventRequests.AddHandler:
                        case GenEventRequests.AddSupHandler:
                            (hibernate, reply) = await Add(
                                context,
                                handlers,
                                a[GenEventRequestLayout.HandlerIndex],
                                a[GenEventRequestLayout.ArgumentsIndex],
                                a.Count == GenEventRequestLayout.AddSupervisedSize ? a[GenEventRequestLayout.SupervisorIndex] : A(ErlangBooleanAtoms.False)
                            );
                            break;
                        case GenEventRequests.DeleteHandler:
                            {
                                int index = Find(handlers, a[GenEventRequestLayout.HandlerIndex]);
                                reply = Error(A(GenEventResults.ModuleNotFound));
                                if (index >= 0)
                                {
                                    GenEventHandler handler = handlers[index];
                                    handlers.RemoveAt(index);
                                    using var deleteSource = GenEventSource.Frame(
                                        context,
                                        GenEventSourceFunctions.ServerDeleteHandler,
                                        GenEventSourceArities.ServerDeleteHandler,
                                        GenEventSourceLines.HandlerDeletion
                                    );
                                    reply = await TerminateHandler(
                                        context,
                                        handler,
                                        a[GenEventRequestLayout.ArgumentsIndex],
                                        A(GenEventTermination.DeleteInput),
                                        name,
                                        A(GenEventTermination.Normal)
                                    );
                                }
                                break;
                            }
                        case GenEventRequests.SwapHandler:
                        case GenEventRequests.SwapSupHandler:
                            (hibernate, reply) = await Swap(
                                context,
                                handlers,
                                a[GenEventRequestLayout.SwapOldHandlerIndex],
                                a[GenEventRequestLayout.SwapOldArgumentsIndex],
                                a[GenEventRequestLayout.SwapNewHandlerIndex],
                                a[GenEventRequestLayout.SwapNewArgumentsIndex],
                                a.Count == GenEventRequestLayout.SwapSupervisedSize ? a[GenEventRequestLayout.SwapSupervisorIndex] : A(ErlangBooleanAtoms.False),
                                name
                            );
                            break;
                        default:
                            {
                                int index = Find(handlers, a[GenEventRequestLayout.HandlerIndex]);
                                reply = Error(A(GenEventResults.BadModule));
                                if (index >= 0)
                                {
                                    using var callSource = GenEventSource.Frame(
                                        context,
                                        GenEventSourceFunctions.ServerCall,
                                        GenEventSourceArities.ServerCall,
                                        GenEventSourceLines.HandlerCallUpdate
                                    );
                                    var updated = await Update(
                                        context,
                                        handlers[index],
                                        GenEventCallbacks.HandleCall,
                                        a[GenEventRequestLayout.ArgumentsIndex],
                                        name
                                    );
                                    hibernate = updated.Hibernate;
                                    reply = updated.Reply;
                                    if (updated.Handler is null)
                                    {
                                        handlers.RemoveAt(index);
                                    }
                                    else
                                    {
                                        handlers[index] = updated.Handler;
                                    }
                                }
                                break;
                            }
                    }
                }
                await External(
                    context,
                    GenEventDependencyModules.Gen,
                    GenEventDependencyFunctions.Reply,
                    request.Items[GenEventMessageLayout.RequestReplyTagIndex],
                    reply
                );
            }
            else
            {
                hibernate = await Notify(
                    context,
                    handlers,
                    GenEventCallbacks.HandleInfo,
                    message,
                    name
                );
            }
            if (hibernate)
            {
                return await Loop(
                    context,
                    parent,
                    name,
                    handlers,
                    timeout,
                    debug,
                    true
                );
            }
        }
    }

    private static int MessageLine(Term message)
    {
        if (message is TupleTerm { Items.Count: GenEventMessageLayout.NotifySize } notification && Is(notification.Items[GenEventMessageLayout.TagIndex], GenEventRequests.Notify))
        {
            return GenEventSourceLines.NotifyMessage;
        }
        if (message is TupleTerm { Items.Count: GenEventMessageLayout.RequestSize } request && request.Items[GenEventMessageLayout.RequestCommandIndex] is TupleTerm command && command.Items.Count > 0
            && command.Items[0] is Atom operation)
        {
            return operation.Name switch
            {
                GenEventRequests.SyncNotify => GenEventSourceLines.SyncNotifyMessage,
                GenEventRequests.Call => GenEventSourceLines.CallMessage,
                GenEventRequests.AddHandler => GenEventSourceLines.AddMessage,
                GenEventRequests.AddSupHandler => GenEventSourceLines.AddSupervisedMessage,
                GenEventRequests.DeleteHandler => GenEventSourceLines.DeleteMessage,
                GenEventRequests.SwapHandler => GenEventSourceLines.SwapMessage,
                GenEventRequests.SwapSupHandler => GenEventSourceLines.SwapSupervisedMessage,
                _ => GenEventSourceLines.OtherMessage
            };
        }

        return GenEventSourceLines.OtherMessage;
    }

    private static bool IsCommand(Term term)
    {
        if (term is Atom atom)
        {
            return atom.Name is GenEventRequests.Stop or GenEventRequests.WhichHandlers or GenEventRequests.GetModules;
        }
        if (term is not TupleTerm tuple || tuple.Items.Count == 0 || tuple.Items[0] is not Atom operation)
        {
            return false;
        }

        return (operation.Name, tuple.Items.Count) is (GenEventRequests.SyncNotify, GenEventRequestLayout.SyncNotifySize) or (GenEventRequests.Call, GenEventRequestLayout.CallSize)
            or (GenEventRequests.AddHandler, GenEventRequestLayout.AddSize) or (GenEventRequests.AddSupHandler, GenEventRequestLayout.AddSupervisedSize) or (GenEventRequests.DeleteHandler, GenEventRequestLayout.DeleteSize)
            or (GenEventRequests.SwapHandler, GenEventRequestLayout.SwapSize) or (GenEventRequests.SwapSupHandler, GenEventRequestLayout.SwapSupervisedSize);
    }

    private static async ValueTask<Term> TerminateServer(
        ProcessContext context,
        Term reason,
        Term parent,
        List<GenEventHandler> handlers,
        Term name
    )
    {
        foreach (GenEventHandler handler in handlers)
        {
            await TerminateHandler(
                context,
                handler,
                A(GenEventTermination.StopInput),
                A(GenEventTermination.StopInput),
                name,
                A(GenEventTermination.Shutdown)
            );
        }
        foreach (GenEventHandler handler in handlers)
        {
            if (handler.Supervisor is Pid && !handler.Supervisor.Equals(parent))
            {
                await External(
                    context,
                    GenEventDependencyModules.Erlang,
                    GenEventDependencyFunctions.Unlink,
                    handler.Supervisor
                );
            }
        }
        throw new ErlangException(reason, ErlangExceptionClasses.Exit);
    }

    private static async ValueTask PrintEvent(
        ProcessContext context,
        Term device,
        Term debug,
        Term name
    )
    {
        string format;
        Term args;
        if (debug is TupleTerm { Items.Count: 2 } incoming && Is(incoming.Items[0], GenEventProtocol.IncomingDebug))
        {
            if (incoming.Items[1] is TupleTerm { Items.Count: 2 } notify && Is(notify.Items[0], GenEventRequests.Notify))
            {
                format = GenEventLogText.DebugEvent;
                args = Term.List(name, notify.Items[1]);
            }
            else if (incoming.Items[1] is TupleTerm { Items.Count: GenEventMessageLayout.RequestSize } request
                && request.Items[GenEventMessageLayout.RequestCommandIndex] is TupleTerm { Items.Count: 3 } call && Is(call.Items[0], GenEventRequests.Call))
            {
                format = GenEventLogText.DebugCall;
                args = Term.List(name, call.Items[1], call.Items[2]);
            }
            else
            {
                format = GenEventLogText.DebugMessage;
                args = Term.List(name, incoming.Items[1]);
            }
        }
        else
        {
            format = GenEventLogText.DebugOther;
            args = Term.List(name, debug);
        }
        await External(
            context,
            GenEventDependencyModules.Io,
            GenEventDependencyFunctions.Format,
            device,
            Term.String(format),
            args
        );
    }
}
