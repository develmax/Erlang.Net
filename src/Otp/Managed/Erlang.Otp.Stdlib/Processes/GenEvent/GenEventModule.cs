// Ported from lib/stdlib/src/gen_event.erl, Erlang/OTP 29.1.1 (Apache-2.0).
// Copyright Ericsson AB 1996-2026. See licenses/ and the retained upstream source.
namespace Erlang;

public static partial class GenEventModule
{
    public const string Name = GenEventLibraryModuleNames.Module;

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (GenEventApi.Start, 0), (GenEventApi.Start, 1), (GenEventApi.Start, 2),
        (GenEventApi.StartLink, 0), (GenEventApi.StartLink, 1), (GenEventApi.StartLink, 2),
        (GenEventApi.StartMonitor, 0), (GenEventApi.StartMonitor, 1), (GenEventApi.StartMonitor, 2),
        (GenEventApi.Stop, 1), (GenEventApi.Stop, 3), (GenEventApi.Notify, 2), (GenEventApi.SyncNotify, 2),
        (GenEventApi.AddHandler, 3), (GenEventApi.AddSupHandler, 3), (GenEventApi.DeleteHandler, 3),
        (GenEventApi.SwapHandler, 3), (GenEventApi.SwapSupHandler, 3), (GenEventApi.WhichHandlers, 1),
        (GenEventApi.Call, 3), (GenEventApi.Call, 4), (GenEventApi.SendRequest, 3), (GenEventApi.SendRequest, 5),
        (GenEventApi.WaitResponse, 2), (GenEventApi.WaitResponse, 3),
        (GenEventApi.ReceiveResponse, 2), (GenEventApi.ReceiveResponse, 3),
        (GenEventApi.CheckResponse, 2), (GenEventApi.CheckResponse, 3),
        (GenEventApi.ReqidsNew, 0), (GenEventApi.ReqidsSize, 1), (GenEventApi.ReqidsAdd, 3), (GenEventApi.ReqidsToList, 1),
        (GenEventApi.WakeHib, 5), (GenEventApi.InitIt, 6), (GenEventApi.SystemContinue, 3),
        (GenEventApi.SystemTerminate, 4), (GenEventApi.SystemCodeChange, 4),
        (GenEventApi.SystemGetState, 1), (GenEventApi.SystemReplaceState, 2),
        (GenEventApi.FormatStatus, 2), (GenEventApi.FormatLog, 1), (GenEventApi.FormatLog, 2), (GenEventApi.BehaviourInfo, 1)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallPreloaded(target =>
        {
            foreach (var export in Exports)
            {
                string function = export.Name;
                target.RegisterReviewedManagedLibrary789(
                    Name,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        });

    private static Atom A(string name) => Term.A(name);

    private static bool Is(Term term, string name) => term is Atom atom && atom.Name == name;

    private static Term Error(Term reason) => Term.Tuple(A(GenEventResults.Error), reason);

    private static ValueTask<Term> External(
        ProcessContext context,
        string module,
        string function,
        params Term[] arguments
    )
        => context.Runtime.Modules.Call(
            context,
            module,
            function,
            arguments
        );

    private static ErlangException BadArgument() => new(ErlangErrorReasons.BadArgument);

    private static ErlangException FunctionClause() => new(ErlangErrorReasons.FunctionClause);

    private static async ValueTask<Term> Caught(
        ProcessContext context,
        Term module,
        string function,
        params Term[] arguments
    )
    {
        try
        {
            if (module is not Atom atom)
            {
                throw BadArgument();
            }

            return await External(
                context,
                atom.Name,
                function,
                arguments
            );
        }
        catch (ErlangException exception)
        {
            if (exception.ExceptionClass == ErlangExceptionClasses.Throw)
            {
                return exception.Reason;
            }
            Term reason = exception.ExceptionClass == ErlangExceptionClasses.Exit
                ? exception.Reason : Term.Tuple(exception.Reason, exception.StackTraceTerm);

            return Term.Tuple(A(GenEventProtocol.Exit), reason);
        }
    }

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        using var tail = context.EnterSourceTailCall(Name, function);
        try
        {
            return await Dispatch(context, function, arguments);
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
            && Is(error.Reason, ErlangErrorReasons.BadArgument) && ArgumentErrorLine(function, arguments.Count) is not null)
        {
            throw GenEventSource.Error(
                context,
                function,
                arguments,
                ErlangErrorReasons.BadArgument,
                ArgumentErrorLine(function, arguments.Count)!.Value
            );
        }
    }

    private static int? ArgumentErrorLine(string function, int arity) => (function, arity) switch
    {
        (GenEventApi.SendRequest, 3) => GenEventSourceLines.SendRequestError,
        (GenEventApi.SendRequest, 5) => GenEventSourceLines.SendCollectionError,
        (GenEventApi.WaitResponse, 2) => GenEventSourceLines.WaitResponseError,
        (GenEventApi.WaitResponse, 3) => GenEventSourceLines.WaitCollectionError,
        (GenEventApi.ReceiveResponse, 2) => GenEventSourceLines.ReceiveResponseError,
        (GenEventApi.ReceiveResponse, 3) => GenEventSourceLines.ReceiveCollectionError,
        (GenEventApi.CheckResponse, 2) => GenEventSourceLines.CheckResponseError,
        (GenEventApi.CheckResponse, 3) => GenEventSourceLines.CheckCollectionError,
        (GenEventApi.ReqidsSize, 1) => GenEventSourceLines.RequestSizeError,
        (GenEventApi.ReqidsAdd, 3) => GenEventSourceLines.RequestAddError,
        (GenEventApi.ReqidsToList, 1) => GenEventSourceLines.RequestListError,
        _ => null
    };

    private static int? ArgumentCallLine(string function, int arity) => (function, arity) switch
    {
        (GenEventApi.SendRequest, 3) => GenEventSourceLines.SendRequestCall,
        (GenEventApi.SendRequest, 5) => GenEventSourceLines.SendCollectionCall,
        (GenEventApi.WaitResponse, 2) => GenEventSourceLines.WaitResponseCall,
        (GenEventApi.WaitResponse, 3) => GenEventSourceLines.WaitCollectionCall,
        (GenEventApi.ReceiveResponse, 2) => GenEventSourceLines.ReceiveResponseCall,
        (GenEventApi.ReceiveResponse, 3) => GenEventSourceLines.ReceiveCollectionCall,
        (GenEventApi.CheckResponse, 2) => GenEventSourceLines.CheckResponseCall,
        (GenEventApi.CheckResponse, 3) => GenEventSourceLines.CheckCollectionCall,
        (GenEventApi.ReqidsSize, 1) => GenEventSourceLines.RequestSizeCall,
        (GenEventApi.ReqidsAdd, 3) => GenEventSourceLines.RequestAddCall,
        (GenEventApi.ReqidsToList, 1) => GenEventSourceLines.RequestListCall,
        _ => null
    };

    private static async ValueTask<Term> Dispatch(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        Term[] a = arguments.ToArray();
        using var wrapperSource = ArgumentCallLine(function, a.Length) is int line
            ? GenEventSource.Frame(
                context,
                function,
                a.Length,
                line
            ) : null;
        switch (function)
        {
            case GenEventApi.Start:
            case GenEventApi.StartLink:
            case GenEventApi.StartMonitor:
                {
                    string mode = function == GenEventApi.Start ? GenEventProtocol.NoLink : function == GenEventApi.StartLink ? GenEventProtocol.Link : GenEventProtocol.Monitor;
                    if (a.Length == 0)
                    {
                        return await External(
                            context,
                            GenEventDependencyModules.Gen,
                            GenEventDependencyFunctions.Start,
                            A(Name),
                            A(mode),
                            A(GenEventProtocol.NoCallbackModule),
                            Nil.Value,
                            Nil.Value
                        );
                    }
                    if (a.Length == 1 && a[0] is Cons or Nil)
                    {
                        return await External(
                            context,
                            GenEventDependencyModules.Gen,
                            GenEventDependencyFunctions.Start,
                            A(Name),
                            A(mode),
                            A(GenEventProtocol.NoCallbackModule),
                            Nil.Value,
                            a[0]
                        );
                    }
                    if (a[0] is not TupleTerm || a.Length == 2 && a[1] is not (Cons or Nil))
                    {
                        throw GenEventSource.Error(
                            context,
                            function,
                            a,
                            ErlangErrorReasons.BadArgument,
                            function == GenEventApi.Start ? (a.Length == 1 ? GenEventSourceLines.StartArgumentError : GenEventSourceLines.StartNamedArgumentError)
                                : function == GenEventApi.StartLink ? (a.Length == 1 ? GenEventSourceLines.StartLinkArgumentError : GenEventSourceLines.StartLinkNamedArgumentError) : (a.Length == 1 ? GenEventSourceLines.StartMonitorArgumentError : GenEventSourceLines.StartMonitorNamedArgumentError)
                        );
                    }

                    return await External(
                        context,
                        GenEventDependencyModules.Gen,
                        GenEventDependencyFunctions.Start,
                        A(Name),
                        A(mode),
                        a[0],
                        A(GenEventProtocol.NoCallbackModule),
                        Nil.Value,
                        a.Length == 2 ? a[1] : Nil.Value
                    );
                }
            case GenEventApi.BehaviourInfo:
                if (Is(a[0], GenEventProtocol.Callbacks))
                {
                    return Term.List(
                        Term.Tuple(A(GenEventCallbacks.Init), Term.I(GenEventCallbackArities.Init)),
                        Term.Tuple(A(GenEventCallbacks.HandleEvent), Term.I(GenEventCallbackArities.HandleEvent)),
                        Term.Tuple(A(GenEventCallbacks.HandleCall), Term.I(GenEventCallbackArities.HandleCall)),
                        Term.Tuple(A(GenEventCallbacks.HandleInfo), Term.I(GenEventCallbackArities.HandleInfo)),
                        Term.Tuple(A(GenEventCallbacks.Terminate), Term.I(GenEventCallbackArities.Terminate)),
                        Term.Tuple(A(GenEventCallbacks.CodeChange), Term.I(GenEventCallbackArities.CodeChange)),
                        Term.Tuple(A(GenEventCallbacks.FormatStatus), Term.I(GenEventCallbackArities.LegacyFormatStatus)),
                        Term.Tuple(A(GenEventCallbacks.FormatStatus), Term.I(GenEventCallbackArities.FormatStatus))
                    );
                }
                if (Is(a[0], GenEventProtocol.OptionalCallbacks))
                {
                    return Term.List(
                        Term.Tuple(A(GenEventCallbacks.HandleInfo), Term.I(GenEventCallbackArities.HandleInfo)),
                        Term.Tuple(A(GenEventCallbacks.Terminate), Term.I(GenEventCallbackArities.Terminate)),
                        Term.Tuple(A(GenEventCallbacks.CodeChange), Term.I(GenEventCallbackArities.CodeChange)),
                        Term.Tuple(A(GenEventCallbacks.FormatStatus), Term.I(GenEventCallbackArities.FormatStatus)),
                        Term.Tuple(A(GenEventCallbacks.FormatStatus), Term.I(GenEventCallbackArities.LegacyFormatStatus))
                    );
                }
                throw GenEventSource.Capture(
                    context,
                    A(ErlangErrorReasons.FunctionClause),
                    [Term.Tuple(
                        A(Name),
                        A(GenEventSourceFunctions.BehaviourInfoClause),
                        Cons.From(a),
                        Nil.Value
                    )]
                );
            case GenEventApi.Stop:
                return await External(
                    context,
                    GenEventDependencyModules.Gen,
                    function,
                    a
                );
            case GenEventApi.Notify:
                {
                    Term message = Term.Tuple(A(GenEventApi.Notify), a[1]);
                    if (a[0] is TupleTerm { Items.Count: 2 } global && Is(global.Items[0], GenEventProtocol.GlobalName))
                    {
                        _ = await Caught(
                            context,
                            A(GenEventProtocol.GlobalName),
                            GenEventDependencyFunctions.Send,
                            global.Items[1],
                            message
                        );
                    }
                    else if (a[0] is TupleTerm { Items.Count: 3 } via && Is(via.Items[0], GenEventProtocol.ViaName))
                    {
                        _ = await Caught(
                            context,
                            via.Items[1],
                            GenEventDependencyFunctions.Send,
                            via.Items[2],
                            message
                        );
                    }
                    else
                    {
                        using var sendSource = GenEventSource.Frame(
                            context,
                            GenEventSourceFunctions.Send,
                            GenEventSourceArities.Send,
                            GenEventSourceLines.Send
                        );
                        await External(
                            context,
                            GenEventDependencyModules.Erlang,
                            GenEventDependencyFunctions.Send,
                            a[0],
                            message
                        );
                    }

                    return A(GenEventResults.Ok);
                }
            case GenEventApi.Call:
                {
                    try
                    {
                        using var callSource = GenEventSource.Frame(
                            context,
                            GenEventSourceFunctions.Call1,
                            a.Length,
                            a.Length == 3 ? GenEventSourceLines.HandlerCall : GenEventSourceLines.HandlerCallWithTimeout
                        );
                        Term result = await External(
                            context,
                            GenEventDependencyModules.Gen,
                            GenEventDependencyFunctions.Call,
                            a.Length == 3 ? [a[0], context.Self, Term.Tuple(A(GenEventApi.Call), a[1], a[2])]
                                : [a[0], context.Self, Term.Tuple(A(GenEventApi.Call), a[1], a[2]), a[3]]
                        );

                        return ((TupleTerm)result).Items[1];
                    }
                    catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit)
                    {
                        throw GenEventSource.Capture(
                            context,
                            Term.Tuple(error.Reason, Term.Tuple(A(Name), A(GenEventApi.Call), Cons.From(a))),
                            [Term.Tuple(
                                A(Name),
                                A(GenEventSourceFunctions.Call1),
                                Term.I(a.Length),
                                GenEventSource.Location(a.Length == 3 ? GenEventSourceLines.HandlerCallExit : GenEventSourceLines.HandlerCallWithTimeoutExit)
                            )],
                            ErlangExceptionClasses.Exit
                        );
                    }
                }
            case GenEventApi.SendRequest:
                return await External(
                    context,
                    GenEventDependencyModules.Gen,
                    function,
                    a.Length == 3 ? [a[0], context.Self, Term.Tuple(A(GenEventApi.Call), a[1], a[2])]
                        : [a[0], context.Self, Term.Tuple(A(GenEventApi.Call), a[1], a[2]), a[3], a[4]]
                );
            case GenEventApi.WaitResponse:
            case GenEventApi.ReceiveResponse:
            case GenEventApi.CheckResponse:
                {
                    Term response = await External(
                        context,
                        GenEventDependencyModules.Gen,
                        function,
                        a
                    );
                    if (a.Length == 3 && response is TupleTerm { Items.Count: 3 } collection)
                    {
                        return Term.Tuple(Unwrap(collection.Items[0]), collection.Items[1], collection.Items[2]);
                    }

                    return Unwrap(response);
                }
            case GenEventApi.ReqidsNew:
            case GenEventApi.ReqidsSize:
            case GenEventApi.ReqidsAdd:
            case GenEventApi.ReqidsToList:
                return await External(
                    context,
                    GenEventDependencyModules.Gen,
                    function,
                    a
                );
            case GenEventApi.InitIt:
                {
                    context.TrapExits = true;
                    Term parent = Is(a[1], GenEventProtocol.SelfParent) ? context.Self : a[1];
                    Term name = await External(
                        context,
                        GenEventDependencyModules.Gen,
                        GenEventDependencyFunctions.Name,
                        a[2]
                    );
                    Term debug = await External(
                        context,
                        GenEventDependencyModules.Gen,
                        GenEventDependencyFunctions.DebugOptions,
                        name,
                        a[5]
                    );
                    Term timeout = await External(
                        context,
                        GenEventDependencyModules.Gen,
                        GenEventDependencyFunctions.HibernateAfter,
                        a[5]
                    );
                    await External(
                        context,
                        GenEventDependencyModules.ProcLib,
                        GenEventDependencyFunctions.InitAck,
                        a[0],
                        Term.Tuple(A(GenEventResults.Ok), context.Self)
                    );

                    return await Loop(
                        context,
                        parent,
                        name,
                        [],
                        timeout,
                        debug,
                        false
                    );
                }
            case GenEventApi.WakeHib:
                return await Fetch(
                    context,
                    a[0],
                    a[1],
                    ReadHandlers(a[2]),
                    a[3],
                    a[4],
                    true
                );
            case GenEventApi.SystemContinue:
                {
                    Term[] state = StateArguments(
                        context,
                        function,
                        a,
                        a[2]
                    );

                    return await Loop(
                        context,
                        a[0],
                        state[GenEventManagerStateLayout.NameIndex],
                        ReadHandlers(state[GenEventManagerStateLayout.HandlersIndex]),
                        state[GenEventManagerStateLayout.TimeoutIndex],
                        a[1],
                        Is(state[GenEventManagerStateLayout.HibernateIndex], ErlangBooleanAtoms.True)
                    );
                }
            case GenEventApi.SystemTerminate:
                {
                    Term[] state = StateArguments(
                        context,
                        function,
                        a,
                        a[3]
                    );

                    return await TerminateServer(
                        context,
                        a[0],
                        a[1],
                        ReadHandlers(state[GenEventManagerStateLayout.HandlersIndex]),
                        state[GenEventManagerStateLayout.NameIndex]
                    );
                }
            case GenEventApi.SystemGetState:
            case GenEventApi.SystemReplaceState:
            case GenEventApi.SystemCodeChange:
                return await SystemState(context, function, a);
            case GenEventApi.FormatStatus:
                return await FormatStatus(context, a[0], a[1]);
            case GenEventApi.FormatLog:
                return await FormatLog(context, a);
        }
        Term command;
        switch (function)
        {
            case GenEventApi.WhichHandlers: command = A(function); break;
            case GenEventApi.SyncNotify: command = Term.Tuple(A(function), a[1]); break;
            case GenEventApi.AddHandler:
            case GenEventApi.DeleteHandler: command = Term.Tuple(A(function), a[1], a[2]); break;
            case GenEventApi.AddSupHandler:
                command = Term.Tuple(
                    A(function),
                    a[1],
                    a[2],
                    context.Self
                ); break;
            case GenEventApi.SwapHandler:
            case GenEventApi.SwapSupHandler:
                {
                    if (a[1] is not TupleTerm { Items.Count: 2 } old || a[2] is not TupleTerm { Items.Count: 2 } next)
                    {
                        throw GenEventSource.Error(
                            context,
                            function,
                            a,
                            ErlangErrorReasons.FunctionClause,
                            function == GenEventApi.SwapHandler ? GenEventSourceLines.SwapClause : GenEventSourceLines.SwapSupervisedClause
                        );
                    }
                    command = function == GenEventApi.SwapHandler
                        ? Term.Tuple(
                            A(function),
                            old.Items[0],
                            old.Items[1],
                            next.Items[0],
                            next.Items[1]
                        )
                        : Term.Tuple(
                            A(function),
                            old.Items[0],
                            old.Items[1],
                            next.Items[0],
                            next.Items[1],
                            context.Self
                        );
                    break;
                }
            default: throw FunctionClause();
        }
        using var rpcSource = GenEventSource.Frame(
            context,
            GenEventSourceFunctions.Rpc,
            GenEventSourceArities.Rpc,
            GenEventSourceLines.RpcCall
        );
        Term reply = await External(
            context,
            GenEventDependencyModules.Gen,
            GenEventDependencyFunctions.Call,
            a[0],
            context.Self,
            command,
            A(GenEventProtocol.Infinity)
        );

        return ((TupleTerm)reply).Items[1];
    }

    private static Term Unwrap(Term term)
        => term is TupleTerm { Items.Count: 2 } response && Is(response.Items[0], GenEventDependencyFunctions.Reply)
            && response.Items[1] is TupleTerm { Items.Count: 2 } error && Is(error.Items[0], GenEventResults.Error)
                ? error : term;

    private static List<GenEventHandler> ReadHandlers(Term term) => Cons.Items(term).Select(GenEventHandler.Decode).ToList();

    private static Term State(
        Term name,
        List<GenEventHandler> handlers,
        Term timeout,
        bool hibernate
    )
        => Term.List(
            name,
            Cons.From(handlers.Select(handler => handler.Encode())),
            timeout,
            A(hibernate ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
        );
}
