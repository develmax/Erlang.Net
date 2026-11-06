// Derived from OTP-29.1.1 lib/kernel/src/rpc.erl, Apache-2.0.
// Copyright Ericsson AB 1996-2025. Original Erlang sources are retained.
namespace Erlang.Otp;

public static partial class RpcLibrary
{
    public const string Name = RpcProtocolNames.Rpc;

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (RpcFunctionNames.Start, 0), (RpcFunctionNames.StartLink, 0), (RpcFunctionNames.Stop, 0), (RpcFunctionNames.Call, 4), (RpcFunctionNames.Call, 5),
        (RpcFunctionNames.BlockCall, 4), (RpcFunctionNames.BlockCall, 5), (RpcFunctionNames.ServerCall, 4), (RpcFunctionNames.Cast, 4),
        (RpcFunctionNames.Abcast, 2), (RpcFunctionNames.Abcast, 3), (RpcFunctionNames.Sbcast, 2), (RpcFunctionNames.Sbcast, 3),
        (RpcFunctionNames.EvalEverywhere, 3), (RpcFunctionNames.EvalEverywhere, 4), (RpcFunctionNames.MultiServerCall, 2), (RpcFunctionNames.MultiServerCall, 3),
        (RpcFunctionNames.Multicall, 3), (RpcFunctionNames.Multicall, 4), (RpcFunctionNames.Multicall, 5), (RpcFunctionNames.AsyncCall, 4),
        (RpcFunctionNames.Yield, 1), (RpcFunctionNames.NbYield, 1), (RpcFunctionNames.NbYield, 2), (RpcFunctionNames.ParallelEval, 1), (RpcFunctionNames.Pmap, 3),
        (RpcFunctionNames.Pinfo, 1), (RpcFunctionNames.Pinfo, 2), (RpcFunctionNames.Init, 1), (RpcFunctionNames.HandleCall, 3), (RpcFunctionNames.HandleCast, 2),
        (RpcFunctionNames.HandleInfo, 2), (RpcFunctionNames.Terminate, 2), (RpcFunctionNames.CodeChange, 3), (RpcFunctionNames.ProxyUserFlush, 0)
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

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        using var tail = context.EnterSourceTailCall(Name, function);
        if (!Exports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        Term a = arguments.Count == 0 ? Nil.Value : arguments[0];
        switch (function)
        {
            case RpcFunctionNames.Call:
                return await Rpcify(context, () => Call(
                    context,
                    RpcDependencyModules.Erpc,
                    RpcDependencyFunctions.Call,
                    arguments.ToArray()
                ));
            case RpcFunctionNames.Cast:
                await ErpcArgumentCall(context, RpcProtocolNames.Cast, arguments.ToArray());

                return Term.A(RpcProtocolNames.True);
            case RpcFunctionNames.AsyncCall:
                return await ErpcArgumentCall(context, RpcProtocolNames.SendRequest, arguments.ToArray());
            case RpcFunctionNames.Yield:
                return await Rpcify(context, () => Call(
                    context,
                    RpcDependencyModules.Erpc,
                    RpcDependencyFunctions.ReceiveResponse,
                    a
                ));
            case RpcFunctionNames.NbYield:
                try
                {
                    Term response = await Call(
                        context,
                        RpcDependencyModules.Erpc,
                        RpcDependencyFunctions.WaitResponse,
                        a,
                        arguments.Count == 1 ? Term.I(0) : arguments[1]
                    );

                    return response is Atom { Name: RpcProtocolNames.NoResponse } ? Term.A(RpcProtocolNames.Timeout)
                        : Term.Tuple(Term.A(RpcProtocolNames.Value), RpcValue(((TupleTerm)response).Items[1]));
                }
                catch (ErlangException error)
                {
                    return await SourceRun(
                        context,
                        RpcFunctionNames.NbYield,
                        2,
                        RpcSourceLines.NbYieldRpcify,
                        () => new ValueTask<Term>(Term.Tuple(Term.A(RpcProtocolNames.Value), RpcException(context, error)))
                    );
                }
            case RpcFunctionNames.BlockCall:
                {
                    Term timeout = arguments.Count == 4 ? Term.A(RpcProtocolNames.Infinity) : arguments[4];
                    if (a is not Atom || arguments[1] is not Atom || arguments[3] is not (Cons or Nil) || !ValidTimeout(timeout))
                    {
                        throw Clause(
                            context,
                            function,
                            arguments.Count == 4 ? arguments.Append(timeout).ToArray() : arguments,
                            RpcSourceLines.BlockCallClause
                        );
                    }

                    return await ServerRequest(
                        context,
                        a,
                        Term.Tuple(
                            Term.A(RpcProtocolNames.BlockCall),
                            arguments[1],
                            arguments[2],
                            arguments[3],
                            await Bif(context, RpcBifNames.GroupLeader)
                        ),
                        timeout
                    );
                }
            case RpcFunctionNames.Multicall:
                {
                    Term nodes, module, method, values, timeout;
                    if (arguments.Count == 5 || arguments.Count == 4 && a is Cons or Nil)
                    {
                        nodes = a;
                        module = arguments[1];
                        method = arguments[2];
                        values = arguments[3];
                        timeout = arguments.Count == 5 ? arguments[4] : Term.A(RpcProtocolNames.Infinity);
                    }
                    else
                    {
                        nodes = await Nodes(context);
                        module = a;
                        method = arguments[1];
                        values = arguments[2];
                        timeout = arguments.Count == 4 ? arguments[3] : Term.A(RpcProtocolNames.Infinity);
                    }
                    Term results = await ErpcArgumentCall(
                        context,
                        RpcProtocolNames.Multicall,
                        nodes,
                        module,
                        method,
                        values,
                        timeout
                    );
                    var good = new List<Term>();
                    var bad = new List<Term>();
                    using var iterator = Cons.Items(results).GetEnumerator();
                    foreach (Term node in Cons.Items(nodes))
                    {
                        if (!iterator.MoveNext())
                        {
                            throw new ErlangException(ErlangErrorReasons.FunctionClause);
                        }
                        var result = (TupleTerm)iterator.Current;
                        if (result.Items[0] is Atom { Name: RpcProtocolNames.Ok })
                        {
                            good.Add(RpcValue(result.Items[1]));
                        }
                        else if (result.Items[1] is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Erpc }, Atom { Name: RpcProtocolNames.Timeout or RpcProtocolNames.Noconnection }] })
                        {
                            bad.Add(node);
                        }
                        else
                        {
                            good.Add(RpcException(context, new ErlangException(result.Items[1], ((Atom)result.Items[0]).Name)));
                        }
                    }

                    return Term.Tuple(Cons.From(good), Cons.From(bad));
                }
            case RpcFunctionNames.ServerCall:
                if (a is not Atom || arguments[1] is not Atom)
                {
                    throw Clause(
                        context,
                        function,
                        arguments,
                        RpcSourceLines.ServerCallClause
                    );
                }

                return await ServerCall(
                    context,
                    a,
                    arguments[1],
                    arguments[2],
                    arguments[3]
                );
            case RpcFunctionNames.Abcast:
                {
                    Term nodes = arguments.Count == 2 ? await Nodes(context) : a;
                    Term name = arguments[^2], message = arguments[^1];
                    while (nodes is Cons cell)
                    {
                        try
                        {
                            await Bif(
                                context,
                                RpcBifNames.Send,
                                Term.Tuple(name, cell.Head),
                                message
                            );
                        }
                        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
                        {
                        }
                        nodes = cell.Tail;
                    }
                    if (nodes is not Nil)
                    {
                        throw Clause(
                            context,
                            function,
                            [nodes, name, message],
                            RpcSourceLines.AbcastClause
                        );
                    }

                    return Term.A(RpcProtocolNames.Abcast);
                }
            case RpcFunctionNames.Sbcast:
            case RpcFunctionNames.MultiServerCall:
                {
                    Term nodes = arguments.Count == 2 ? await Nodes(context) : a;
                    Term name = arguments[^2], message = arguments[^1];
                    if (function == RpcFunctionNames.MultiServerCall && (nodes is not (Cons or Nil) || name is not Atom))
                    {
                        throw Clause(
                            context,
                            function,
                            arguments,
                            RpcSourceLines.MultiServerClause
                        );
                    }

                    return await SourceRun(
                        context,
                        function,
                        3,
                        function == RpcFunctionNames.Sbcast ? RpcSourceLines.SbcastSendNodes : RpcSourceLines.MultiServerSendNodes,
                        () => Broadcast(
                            context,
                            nodes,
                            function == RpcFunctionNames.Sbcast ? Term.A(RpcProtocolNames.Rex) : name,
                            function == RpcFunctionNames.Sbcast ? Term.Tuple(Term.A(RpcProtocolNames.Sbcast), name, message) : message
                        )
                    );
                }
            case RpcFunctionNames.EvalEverywhere:
                {
                    Term nodes = arguments.Count == 3 ? await Nodes(context) : a;
                    FunctionTerm action = new RpcCastRecipe(context.Runtime.Functions).Rebind([arguments[^3], arguments[^2], arguments[^1]]);
                    await SourceRun(
                        context,
                        function,
                        4,
                        RpcSourceLines.EvalForEach,
                        () => Call(
                            context,
                            RpcDependencyModules.Lists,
                            RpcDependencyFunctions.ForEach,
                            action,
                            nodes
                        )
                    );

                    return Term.A(RpcProtocolNames.Abcast);
                }
            case RpcFunctionNames.ParallelEval:
                return await Parallel(context, a);
            case RpcFunctionNames.Pmap:
                {
                    if (a is not TupleTerm { Items.Count: 2 } mfa)
                    {
                        throw Clause(
                            context,
                            function,
                            arguments,
                            RpcSourceLines.PmapClause
                        );
                    }
                    Term evaluated;
                    using (context.EnterGeneratedFunction(
                        Name,
                        function,
                        3,
                        Location(RpcSourceLines.PmapBuildArguments),
                        new object()
                    ))
                    {
                        Term calls = Nil.Value;
                        Term remaining = arguments[2];
                        while (remaining is Cons cell)
                        {
                            calls = new Cons(Term.Tuple(mfa.Items[0], mfa.Items[1], new Cons(cell.Head, arguments[1])), calls);
                            remaining = cell.Tail;
                        }
                        if (remaining is not Nil || mfa.Items[0] is not Atom || mfa.Items[1] is not Atom)
                        {
                            throw Clause(
                                context,
                                RpcFunctionNames.BuildArgs,
                                [mfa.Items[0], mfa.Items[1], arguments[1], remaining, calls],
                                RpcSourceLines.BuildArgumentsClause
                            );
                        }
                        evaluated = await Parallel(context, calls);
                    }
                    var results = Cons.Items(evaluated).ToList();
                    if (results.Any(value => value is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Badrpc }, _] }))
                    {
                        throw SourceError(
                            context,
                            RpcFunctionNames.Check,
                            2,
                            RpcSourceLines.CheckFailure,
                            RpcProtocolNames.Badrpc,
                            ErlangExceptionClasses.Exit
                        );
                    }
                    results.Reverse();

                    return Cons.From(results);
                }
            case RpcFunctionNames.Pinfo:
                {
                    Term node = await SourceRun(
                        context,
                        function,
                        arguments.Count,
                        arguments.Count == 1 ? RpcSourceLines.PinfoNode : RpcSourceLines.PinfoItemNode,
                        () => Bif(context, RpcBifNames.Node, a)
                    );
                    if (node.Equals(context.Runtime.NodeAtom))
                    {
                        return await SourceRun(
                            context,
                            function,
                            arguments.Count,
                            arguments.Count == 1 ? RpcSourceLines.PinfoLocal : RpcSourceLines.PinfoItemLocal,
                            () => Bif(context, RpcBifNames.ProcessInfo, arguments.ToArray())
                        );
                    }

                    return await Invoke(
                        context,
                        arguments.Count == 1 ? RpcProtocolNames.Call : RpcProtocolNames.BlockCall,
                        [node, Term.A(RpcProtocolNames.Erlang), Term.A(RpcProtocolNames.ProcessInfo), Cons.From(arguments)]
                    );
                }
            default:
                return await Server(context, function, arguments);
        }
    }

    private static async ValueTask<Term> Parallel(ProcessContext context, Term calls)
    {
        Term[] nodes = Cons.Items(await Nodes(context)).ToArray();
        var keys = new List<Term>();
        var continuations = new List<IDisposable>();
        using (context.EnterGeneratedFunction(
            Name,
            RpcFunctionNames.ParallelEval,
            1,
            Location(RpcSourceLines.ParallelMapNodes),
            new object()
        ))
        {
            try
            {
                while (calls is not Nil)
                {
                    int index = keys.Count % nodes.Length;
                    if (calls is not Cons cell || cell.Head is not TupleTerm { Items.Count: 3 } mfa)
                    {
                        throw Clause(
                            context,
                            RpcFunctionNames.MapNodes,
                            [calls, Cons.From(nodes.Skip(index)), Cons.From(nodes)],
                            RpcSourceLines.MapNodesClause
                        );
                    }
                    Term key = await SourceRun(
                        context,
                        RpcFunctionNames.MapNodes,
                        3,
                        RpcSourceLines.MapNodesAsyncCall,
                        () => Invoke(context, RpcFunctionNames.AsyncCall, [nodes[index], mfa.Items[0], mfa.Items[1], mfa.Items[2]])
                    );
                    keys.Add(key);
                    calls = cell.Tail;
                    continuations.Add(context.EnterGeneratedFunction(
                        Name,
                        RpcFunctionNames.MapNodes,
                        3,
                        Location(RpcSourceLines.MapNodesRecurse),
                        new object()
                    ));
                }
            }
            finally
            {
                for (int index = continuations.Count - 1; index >= 0; index--)
                {
                    continuations[index].Dispose();
                }
            }
        }
        var values = new List<Term>();
        foreach (Term key in keys)
        {
            values.Add(await Invoke(context, RpcFunctionNames.Yield, [key]));
        }

        return Cons.From(values);
    }

    private static async ValueTask<Term> ErpcArgumentCall(ProcessContext context, string function, params Term[] arguments)
    {
        try
        {
            return await Call(
                context,
                RpcProtocolNames.Erpc,
                function,
                arguments
            );
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
            && error.Reason is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Erpc }, Atom { Name: RpcProtocolNames.Badarg }] })
        {
            throw SourceError(
                context,
                function == RpcFunctionNames.SendRequest ? RpcProtocolNames.AsyncCall : function,
                function == RpcFunctionNames.Multicall ? 5 : 4,
                function == RpcFunctionNames.SendRequest ? RpcSourceLines.AsyncBadArgument : function == RpcFunctionNames.Multicall ? RpcSourceLines.MulticallBadArgument : RpcSourceLines.CastBadArgument,
                ErlangErrorReasons.BadArgument
            );
        }
    }

    private static async ValueTask<Term> Rpcify(ProcessContext context, Func<ValueTask<Term>> body)
    {
        try
        {
            return RpcValue(await body());
        }
        catch (ErlangException error)
        {
            return RpcException(context, error);
        }
    }

    private static Term RpcValue(Term value)
        => value is TupleTerm { Items: [Atom { Name: RpcProtocolNames.ExitTag }, _] } ? Term.Tuple(Term.A(RpcProtocolNames.Badrpc), value) : value;

    private static Term RpcException(ProcessContext context, ErlangException error)
    {
        Term reason = error.Reason;
        if (error.ExceptionClass == ErlangExceptionClasses.Throw)
        {
            return RpcValue(reason);
        }
        if (error.ExceptionClass == ErlangExceptionClasses.Exit)
        {
            if (reason is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Exception or RpcProtocolNames.Signal }, var inner] })
            {
                return BadExit(inner);
            }
            throw SourceFailure(
                context,
                RpcFunctionNames.RpcifyException,
                2,
                RpcSourceLines.RpcifyExit,
                reason,
                ErlangExceptionClasses.Exit
            );
        }
        if (reason is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Exception }, var failure, var stack] })
        {
            return BadExit(Term.Tuple(failure, stack));
        }
        if (reason is TupleTerm { Items: [Atom { Name: RpcProtocolNames.Erpc }, var remote] })
        {
            return remote switch
            {
                Atom { Name: RpcProtocolNames.Badarg } => throw SourceError(
                    context,
                    RpcFunctionNames.RpcifyException,
                    2,
                    RpcSourceLines.RpcifyBadArgument,
                    ErlangErrorReasons.BadArgument
                ),
                Atom { Name: RpcProtocolNames.Noconnection } => Term.Tuple(Term.A(RpcProtocolNames.Badrpc), Term.A(RpcProtocolNames.Nodedown)),
                Atom { Name: RpcProtocolNames.Timeout or RpcProtocolNames.Notsup } => Term.Tuple(Term.A(RpcProtocolNames.Badrpc), remote),
                _ => BadExit(remote)
            };
        }
        throw SourceFailure(
            context,
            RpcFunctionNames.RpcifyException,
            2,
            RpcSourceLines.RpcifyError,
            reason
        );
    }

    private static Term BadExit(Term reason) => Term.Tuple(Term.A(RpcProtocolNames.Badrpc), Term.Tuple(Term.A(RpcProtocolNames.ExitTag), reason));

    private static ValueTask<Term> Call(
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

    private static ValueTask<Term> Bif(ProcessContext context, string function, params Term[] arguments)
        => Call(
            context,
            RpcProtocolNames.Erlang,
            function,
            arguments
        );

    private static async ValueTask<Term> Nodes(ProcessContext context)
        => new Cons(context.Runtime.NodeAtom, await Bif(context, RpcBifNames.Nodes));

    private static bool ProperList(Term value)
    {
        while (value is Cons cell)
        {
            value = cell.Tail;
        }

        return value is Nil;
    }

    private static bool ValidTimeout(Term value)
        => value is Atom { Name: RpcProtocolNames.Infinity } || value is Integer number && number.Value >= 0 && number.Value <= RpcTimeout.MaximumMilliseconds;

    private static ErlangException Clause(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int line
    )
    {
        using var frame = context.EnterGeneratedFunction(
            Name,
            function,
            arguments.Count,
            Location(line),
            new object()
        );

        return new ErlangException(ErlangErrorReasons.FunctionClause) { StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), null), ExplicitStackTrace = true };
    }

    private static ErlangException SourceError(
        ProcessContext context,
        string function,
        int arity,
        int line,
        string reason,
        string exceptionClass = ErlangExceptionClasses.Error
    )
    {
        return SourceFailure(
            context,
            function,
            arity,
            line,
            Term.A(reason),
            exceptionClass
        );
    }

    private static ErlangException SourceFailure(
        ProcessContext context,
        string function,
        int arity,
        int line,
        Term reason,
        string exceptionClass = ErlangExceptionClasses.Error
    )
    {
        using var frame = context.EnterGeneratedFunction(
            Name,
            function,
            arity,
            Location(line),
            new object()
        );

        return new ErlangException(reason, exceptionClass) { StackTraceTerm = context.CaptureErrorStack(Term.I(arity), null), ExplicitStackTrace = true };
    }

    private static async ValueTask<Term> SourceRun(
        ProcessContext context,
        string function,
        int arity,
        int line,
        Func<ValueTask<Term>> body
    )
    {
        using var frame = context.EnterGeneratedFunction(
            Name,
            function,
            arity,
            Location(line),
            new object()
        );
        try
        {
            return await body();
        }
        catch (ErlangException error)
        {
            throw context.CaptureSourceFailure(error);
        }
    }

    private static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(RpcProtocolNames.File), Term.String(RpcProtocolNames.FileName)),
        Term.Tuple(Term.A(RpcProtocolNames.Line), Term.I(line))
    );
}
