// Derived from OTP-29.1.1 rpc.erl; Apache-2.0, Ericsson AB.
namespace Erlang.Otp;

public static partial class RpcLibrary
{
    private static ValueTask<Term> Spawn(
        ProcessContext context,
        string operation,
        RpcWorkerKind kind,
        IReadOnlyList<Term> environment
    )
        => Bif(context, operation, new RpcWorkerRecipe(context.Runtime.Functions, kind).Rebind(environment));

    private static async ValueTask<Term> Server(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        switch (function)
        {
            case RpcFunctionNames.Start:
            case RpcFunctionNames.StartLink:
                return await Call(
                    context,
                    RpcProtocolNames.GenServer,
                    function,
                    Term.Tuple(Term.A(RpcProtocolNames.Local), Term.A(RpcProtocolNames.Rex)),
                    Term.A(Name),
                    Nil.Value,
                    Term.List(Term.Tuple(
                        Term.A(RpcProtocolNames.SpawnOpt),
                        Term.List(Term.Tuple(Term.A(RpcProtocolNames.MessageQueueData), Term.A(RpcProtocolNames.OffHeap)))
                    ))
                );
            case RpcFunctionNames.Stop:
                return await Call(
                    context,
                    RpcDependencyModules.GenServer,
                    RpcDependencyFunctions.Call,
                    Term.A(RpcProtocolNames.Rex),
                    Term.A(RpcProtocolNames.Stop),
                    Term.A(RpcProtocolNames.Infinity)
                );
            case RpcFunctionNames.Init:
                if (arguments[0] is not Nil)
                {
                    throw Clause(
                        context,
                        function,
                        arguments,
                        RpcSourceLines.InitClause
                    );
                }
                await Bif(
                    context,
                    RpcBifNames.ProcessFlag,
                    Term.A(RpcProtocolNames.TrapExit),
                    Term.A(RpcProtocolNames.True)
                );

                return Term.Tuple(
                    Term.A(RpcProtocolNames.Ok),
                    new MapTerm(new Dictionary<Term, Term>
                    {
                        [Term.A(RpcProtocolNames.NodesObserver)] = await Spawn(
                            context,
                            RpcProtocolNames.SpawnMonitor,
                            RpcWorkerKind.NodesObserver,
                            []
                        )
                    })
                );
            case RpcFunctionNames.HandleCall:
                {
                    Term request = arguments[0], to = arguments[1], state = arguments[2];
                    if (request is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Call }, var module, var method, var values, var leader] })
                    {
                        try
                        {
                            var spawned = (TupleTerm)await Spawn(
                                context,
                                RpcProtocolNames.SpawnMonitor,
                                RpcWorkerKind.Execute,
                                [module, method, values, leader, to]
                            );

                            return NoReply(await Call(
                                context,
                                RpcDependencyModules.Maps,
                                RpcDependencyFunctions.Put,
                                spawned.Items[1],
                                to,
                                state
                            ));
                        }
                        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason is Atom { Name: RpcProtocolNames.SystemLimit })
                        {
                            return Term.Tuple(Term.A(RpcProtocolNames.Reply), BadExit(Term.A(RpcProtocolNames.SystemLimit)), state);
                        }
                    }
                    if (request is TupleTerm { Items: [Atom { Name: RpcProtocolNames.BlockCall }, var mod, var fun, var args, var gl] })
                    {
                        Term before = await Bif(context, RpcBifNames.GroupLeader);
                        await SourceRun(
                            context,
                            RpcFunctionNames.HandleCall,
                            3,
                            RpcSourceLines.BlockGroupLeader,
                            () => SetGroupLeader(context, gl)
                        );
                        Term result = await ExecuteCall(
                            context,
                            mod,
                            fun,
                            args
                        );
                        await Bif(
                            context,
                            RpcBifNames.GroupLeader,
                            before,
                            context.Self
                        );

                        return Term.Tuple(Term.A(RpcProtocolNames.Reply), result, state);
                    }

                    return request is Atom { Name: RpcProtocolNames.Stop }
                        ? Term.Tuple(
                            Term.A(RpcProtocolNames.Stop),
                            Term.A(RpcProtocolNames.Normal),
                            Term.A(RpcProtocolNames.Stopped),
                            state
                        ) : NoReply(state);
                }
            case RpcFunctionNames.HandleCast:
                if (arguments[0] is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Cast }, var castModule, var castMethod, var castArgs, var castLeader] })
                {
                    try
                    {
                        await Spawn(
                            context,
                            RpcProtocolNames.Spawn,
                            RpcWorkerKind.Cast,
                            [castModule, castMethod, castArgs, castLeader]
                        );
                    }
                    catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason is Atom { Name: RpcProtocolNames.SystemLimit })
                    {
                    }
                }

                return NoReply(arguments[1]);
            case RpcFunctionNames.HandleInfo:
                return await HandleInfo(context, arguments[0], arguments[1]);
            case RpcFunctionNames.Terminate:
                return Term.A(RpcProtocolNames.Ok);
            case RpcFunctionNames.CodeChange:
                return Term.Tuple(Term.A(RpcProtocolNames.Ok), arguments[1]);
            case RpcFunctionNames.ProxyUserFlush:
                return await ProxyFlush(context);
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    private static Term NoReply(Term state) => Term.Tuple(Term.A(RpcProtocolNames.Noreply), state);

    private static async ValueTask<Term> HandleInfo(ProcessContext context, Term message, Term state)
    {
        if (message is TupleTerm { Items: [Atom { Name: RpcProtocolNames.DOWN }, var monitor, Atom { Name: RpcProtocolNames.Process }, var pid, var reason] })
        {
            if (state is MapTerm map && map.TryGet(Term.A(RpcProtocolNames.NodesObserver), out Term? observer)
                && observer is TupleTerm { Items: [var observedPid, var observedMonitor] } && observedPid.Equals(pid) && observedMonitor.Equals(monitor))
            {
                return NoReply(await Call(
                    context,
                    RpcDependencyModules.Maps,
                    RpcDependencyFunctions.Put,
                    Term.A(RpcProtocolNames.NodesObserver),
                    await Spawn(
                        context,
                        RpcProtocolNames.SpawnMonitor,
                        RpcWorkerKind.NodesObserver,
                        []
                    ),
                    state
                ));
            }
            if (reason is not Atom { Name: RpcProtocolNames.Normal })
            {
                if (state is not MapTerm pending)
                {
                    throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), state));
                }
                Term to = pending.TryGet(monitor, out Term? destination) ? destination! : Term.A(RpcProtocolNames.Undefined);
                if (to is Atom { Name: RpcProtocolNames.Undefined })
                {
                    return NoReply(state);
                }
                else
                {
                    if (to is not TupleTerm { Items.Count: 2 })
                    {
                        using var frame = context.EnterGeneratedFunction(
                            Name,
                            RpcFunctionNames.HandleInfo,
                            2,
                            Location(RpcSourceLines.DownDestinationCase),
                            new object()
                        );
                        throw context.CaptureSourceFailure(new ErlangException(Term.Tuple(Term.A(RpcProtocolNames.CaseClause), to)));
                    }
                    await SourceRun(
                        context,
                        RpcFunctionNames.HandleInfo,
                        2,
                        RpcSourceLines.DownReply,
                        () => Reply(context, to, BadExit(reason))
                    );
                }
            }

            return NoReply(await SourceRun(
                context,
                RpcFunctionNames.HandleInfo,
                2,
                reason is Atom { Name: RpcProtocolNames.Normal } ? RpcSourceLines.NormalDownRemove : RpcSourceLines.FailedDownRemove,
                () => Call(
                    context,
                    RpcDependencyModules.Maps,
                    RpcDependencyFunctions.Remove,
                    monitor,
                    state
                )
            ));
        }
        if (message is TupleTerm { Items: [var from, TupleTerm { Items: [Atom { Name: RpcProtocolNames.Sbcast or RpcProtocolNames.Send } operation, var name, var payload] }] })
        {
            Term sent = await Catch(() => Bif(
                context,
                RpcBifNames.Send,
                name,
                operation.Name == RpcProtocolNames.Sbcast ? payload : Term.Tuple(from, payload)
            ));
            if (sent is TupleTerm { Items: [Atom { Name: RpcProtocolNames.ExitTag }, _] })
            {
                await SourceRun(
                    context,
                    RpcFunctionNames.HandleInfo,
                    2,
                    operation.Name == RpcProtocolNames.Sbcast ? RpcSourceLines.SbcastMissingName : RpcSourceLines.SendMissingName,
                    () => Bif(
                        context,
                        RpcBifNames.Send,
                        from,
                        Term.Tuple(Term.A(RpcProtocolNames.Rex), context.Runtime.NodeAtom, Term.Tuple(Term.A(RpcProtocolNames.NonexistingName), name))
                    )
                );
            }
            else if (operation.Name == RpcProtocolNames.Sbcast)
            {
                await Bif(
                    context,
                    RpcBifNames.Send,
                    from,
                    Term.Tuple(Term.A(RpcProtocolNames.Rex), context.Runtime.NodeAtom, context.Runtime.NodeAtom)
                );
            }

            return NoReply(state);
        }
        if (message is TupleTerm { Items: [var caller, TupleTerm { Items: [Atom { Name: RpcProtocolNames.Call }, var module, var method, var values, var leader] }] })
        {
            Term to = Term.Tuple(Term.A(RpcProtocolNames.Rex), caller);
            Term groupLeader = leader is Atom { Name: RpcProtocolNames.SendStdoutToCaller } ? Term.Tuple(leader, caller) : leader;
            var result = (TupleTerm)await Server(
                context,
                RpcFunctionNames.HandleCall,
                [Term.Tuple(
                    Term.A(RpcProtocolNames.Call),
                    module,
                    method,
                    values,
                    groupLeader
                ), to, state]
            );
            if (result.Items.Count == 3)
            {
                await Reply(context, to, result.Items[1]);

                return NoReply(result.Items[2]);
            }

            return result;
        }
        if (message is TupleTerm { Items: [var requester, Atom { Name: RpcProtocolNames.FeaturesRequest }] })
        {
            await SourceRun(
                context,
                RpcFunctionNames.HandleInfo,
                2,
                RpcSourceLines.FeaturesReply,
                () => Bif(
                    context,
                    RpcBifNames.Send,
                    requester,
                    Term.Tuple(Term.A(RpcProtocolNames.FeaturesReply), context.Runtime.NodeAtom, Term.List(Term.A(RpcProtocolNames.Erpc)))
                )
            );
        }

        return NoReply(state);
    }

    private static async ValueTask<Term> Reply(ProcessContext context, Term to, Term result)
    {
        if (to is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Rex }, var from] })
        {
            await SourceRun(
                context,
                RpcFunctionNames.Reply,
                2,
                RpcSourceLines.ReplySend,
                () => Bif(
                    context,
                    RpcBifNames.Send,
                    from,
                    Term.Tuple(Term.A(RpcProtocolNames.Rex), result)
                )
            );

            return Term.A(RpcProtocolNames.Ok);
        }

        if (to is not TupleTerm { Items: [Pid, _] })
        {
            throw Clause(
                context,
                RpcFunctionNames.Reply,
                [to, result],
                RpcSourceLines.ReplyClause
            );
        }

        return await Call(
            context,
            RpcDependencyModules.GenServer,
            RpcDependencyFunctions.Reply,
            to,
            result
        );
    }

    private static async ValueTask<Term> ExecuteCall(
        ProcessContext context,
        Term module,
        Term method,
        Term values
    )
    {
        try
        {
            var result = (TupleTerm)await Call(
                context,
                RpcDependencyModules.Erpc,
                RpcDependencyFunctions.ExecuteCall,
                module,
                method,
                values
            );

            return result.Items[1];
        }
        catch (ErlangException error)
        {
            if (error.ExceptionClass == ErlangExceptionClasses.Throw)
            {
                return error.Reason;
            }
            if (error.ExceptionClass == ErlangExceptionClasses.Exit)
            {
                return BadExit(error.Reason);
            }
            Term argumentError = await Call(
                context,
                RpcDependencyModules.Erpc,
                RpcDependencyFunctions.IsArgError,
                error.Reason,
                module,
                method,
                values
            );
            if (argumentError is Atom { Name: RpcProtocolNames.True })
            {
                return BadExit(error.Reason);
            }
            Term stack = await Call(
                context,
                RpcDependencyModules.Erpc,
                RpcDependencyFunctions.TrimStack,
                error.StackTraceTerm,
                module,
                method,
                values
            );

            return BadExit(Term.Tuple(error.Reason, stack));
        }
    }

    private static async ValueTask<Term> SetGroupLeader(ProcessContext context, Term leader)
    {
        if (leader is Pid)
        {
            return await Bif(
                context,
                RpcBifNames.GroupLeader,
                leader,
                context.Self
            );
        }
        if (leader is TupleTerm { Items: [Atom { Name: RpcProtocolNames.SendStdoutToCaller }, var caller] })
        {
            Term proxy = await Spawn(
                context,
                RpcProtocolNames.SpawnLink,
                RpcWorkerKind.CnodeGroupLeader,
                [caller]
            );

            return await Bif(
                context,
                RpcBifNames.GroupLeader,
                proxy,
                context.Self
            );
        }
        if (leader is Atom { Name: RpcProtocolNames.User })
        {
            Term user = await Bif(context, RpcBifNames.Whereis, leader);
            while (user is not Pid)
            {
                user = await Bif(context, RpcBifNames.Whereis, Term.A(RpcProtocolNames.RexProxyUser));
                if (user is Pid)
                {
                    break;
                }
                Term proxy = await Spawn(
                    context,
                    RpcProtocolNames.Spawn,
                    RpcWorkerKind.ProxyUser,
                    []
                );
                try
                {
                    await Bif(
                        context,
                        RpcBifNames.Register,
                        Term.A(RpcProtocolNames.RexProxyUser),
                        proxy
                    );
                    user = proxy;
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
                {
                    await Bif(
                        context,
                        RpcBifNames.Exit,
                        proxy,
                        Term.A(RpcProtocolNames.Kill)
                    );
                }
            }

            return await Bif(
                context,
                RpcBifNames.GroupLeader,
                user,
                context.Self
            );
        }
        throw Clause(
            context,
            RpcFunctionNames.SetGroupLeader,
            [leader],
            RpcSourceLines.GroupLeaderClause
        );
    }

    internal static async ValueTask<Term> RunWorker(ProcessContext context, RpcWorkerKind kind, IReadOnlyList<Term> environment)
    {
        switch (kind)
        {
            case RpcWorkerKind.Execute:
                await SetGroupLeader(context, environment[3]);
                Term leader = await Bif(context, RpcBifNames.GroupLeader);
                Term result = await ExecuteCall(
                    context,
                    environment[0],
                    environment[1],
                    environment[2]
                );
                if (environment[3] is TupleTerm { Items: [Atom { Name: RpcProtocolNames.SendStdoutToCaller }, _] })
                {
                    Term reference = await Bif(context, RpcBifNames.MakeRef);
                    await Bif(
                        context,
                        RpcBifNames.Send,
                        leader,
                        Term.Tuple(
                            Term.A(RpcProtocolNames.Stop),
                            context.Self,
                            reference,
                            environment[4],
                            result
                        )
                    );
                    await context.ReceiveErlangAsync<Term>(value => value.Equals(reference) ? value : null, Term.A(RpcProtocolNames.Infinity));

                    return Term.A(RpcProtocolNames.Ok);
                }

                return await Reply(context, environment[4], result);
            case RpcWorkerKind.Cast:
                await SetGroupLeader(context, environment[3]);

                return await Call(
                    context,
                    RpcDependencyModules.Erpc,
                    RpcDependencyFunctions.ExecuteCast,
                    environment[0],
                    environment[1],
                    environment[2]
                );
            case RpcWorkerKind.Middleman:
                await Bif(
                    context,
                    RpcBifNames.ProcessFlag,
                    Term.A(RpcProtocolNames.TrapExit),
                    Term.A(RpcProtocolNames.True)
                );
                Term reply = await Call(
                    context,
                    RpcDependencyModules.GenServer,
                    RpcDependencyFunctions.Call,
                    Term.Tuple(Term.A(RpcProtocolNames.Rex), environment[0]),
                    environment[1],
                    environment[2]
                );
                throw new ErlangException(Term.Tuple(context.Self, environment[3], reply), ErlangExceptionClasses.Exit);
            case RpcWorkerKind.NodesObserver:
                return await NodesObserver(context);
            case RpcWorkerKind.ProxyUser:
                do
                {
                    await context.ReceiveErlangAsync<Term>(_ => null, Term.I(RpcTiming.ProxyPollMilliseconds));
                }
                while (await Bif(context, RpcBifNames.Whereis, Term.A(RpcProtocolNames.User)) is not Pid);

                return await ProxyFlush(context);
            case RpcWorkerKind.CnodeGroupLeader:
                return await CnodeLoop(context, Term.Tuple(Term.A(RpcProtocolNames.CnodeCallGroupLeaderState), environment[0]));
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static async ValueTask<Term> ProxyFlush(ProcessContext context)
    {
        while (true)
        {
            Term? message = await context.ReceiveErlangAsync<Term>(value => value, Term.I(RpcTiming.ProxyHibernateMilliseconds));
            if (message is null)
            {
                return await Bif(
                    context,
                    RpcBifNames.Hibernate,
                    Term.A(Name),
                    Term.A(RpcProtocolNames.ProxyUserFlush),
                    Nil.Value
                );
            }
            await Bif(
                context,
                RpcBifNames.Send,
                Term.A(RpcProtocolNames.User),
                message
            );
        }
    }

    private static async ValueTask<Term> NodesObserver(ProcessContext context)
    {
        await Bif(
            context,
            RpcBifNames.ProcessFlag,
            Term.A(RpcProtocolNames.Priority),
            Term.A(RpcProtocolNames.High)
        );
        await Bif(
            context,
            RpcBifNames.ProcessFlag,
            Term.A(RpcProtocolNames.TrapExit),
            Term.A(RpcProtocolNames.True)
        );
        Term table = await Call(
            context,
            RpcDependencyModules.Ets,
            RpcDependencyFunctions.New,
            Term.A(RpcProtocolNames.RexNodesObserver),
            Term.List(Term.Tuple(Term.A(RpcProtocolNames.ReadConcurrency), Term.A(RpcProtocolNames.True)), Term.A(RpcProtocolNames.Protected))
        );
        await Call(
            context,
            RpcDependencyModules.PersistentTerm,
            RpcDependencyFunctions.Put,
            Term.A(RpcProtocolNames.RexNodesObserver),
            table
        );
        await Call(
            context,
            RpcDependencyModules.NetKernel,
            RpcDependencyFunctions.MonitorNodes,
            Term.A(RpcProtocolNames.True)
        );
        foreach (Term node in Cons.Items(await Nodes(context)))
        {
            await Bif(
                context,
                RpcBifNames.Send,
                context.Self,
                Term.Tuple(Term.A(RpcProtocolNames.Nodeup), node)
            );
        }
        while (true)
        {
            Term message = (await context.ReceiveErlangAsync<Term>(value => value, Term.A(RpcProtocolNames.Infinity)))!;
            switch (message)
            {
                case TupleTerm { Items: [Atom { Name: RpcProtocolNames.Nodeup }, Atom { Name: RpcProtocolNames.NonodeNohost }] }:
                    break;
                case TupleTerm { Items: [Atom { Name: RpcProtocolNames.Nodeup }, var node] }:
                    await Bif(
                        context,
                        RpcBifNames.Send,
                        Term.Tuple(Term.A(RpcProtocolNames.Rex), node),
                        Term.Tuple(context.Self, Term.A(RpcProtocolNames.FeaturesRequest))
                    );
                    break;
                case TupleTerm { Items: [Atom { Name: RpcProtocolNames.Nodedown }, var downNode] }:
                    await Call(
                        context,
                        RpcDependencyModules.Ets,
                        RpcDependencyFunctions.Delete,
                        table,
                        downNode
                    );
                    break;
                case TupleTerm { Items: [Atom { Name: RpcProtocolNames.FeaturesReply }, var from, var features] }:
                    try
                    {
                        Term supported = await Call(
                            context,
                            RpcDependencyModules.Lists,
                            RpcDependencyFunctions.Member,
                            Term.A(RpcProtocolNames.Erpc),
                            features
                        );
                        await Call(
                            context,
                            RpcDependencyModules.Ets,
                            RpcDependencyFunctions.Insert,
                            table,
                            Term.Tuple(from, supported)
                        );
                    }
                    catch (ErlangException)
                    {
                        await Call(
                            context,
                            RpcDependencyModules.Ets,
                            RpcDependencyFunctions.Insert,
                            table,
                            Term.Tuple(from, Term.A(RpcProtocolNames.False))
                        );
                    }
                    break;
            }
        }
    }
}
