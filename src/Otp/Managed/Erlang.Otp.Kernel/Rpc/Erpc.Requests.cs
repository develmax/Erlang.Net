// Derived from OTP 29.1.1 erpc.erl, Apache-2.0. Copyright Ericsson AB 2020-2025.
namespace Erlang.Otp;

internal static partial class Erpc
{
    private static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        using var tail = context.EnterSourceTailCall(ErpcNames.Module, function);
        try
        {
            return await InvokeCore(context, function, arguments);
        }
        catch (ErlangException error)
        {
            if (error.AutomaticStackTraceCaptured || error.ExplicitStackTrace)
                throw;
            var (name, arity, line) = FailureLocation(function, arguments, error);

            throw ErpcSource.Capture(
                context,
                error,
                name,
                arity,
                line
            );
        }
    }

    private static async ValueTask<Term> InvokeCore(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        switch (function)
        {
            case ErpcNames.Call:
                return await Call(context, ExpandFunction(arguments, true));
            case ErpcNames.SendRequest:
                return await SendRequest(context, arguments);
            case ErpcNames.ReceiveResponse:
            case ErpcNames.WaitResponse:
                return await ReceiveResponse(context, arguments, function == ErpcNames.WaitResponse);
            case ErpcNames.CheckResponse:
                return await CheckResponse(context, arguments);
            case ErpcNames.RequestIdsNew:
                return new MapTerm([]);
            case ErpcNames.RequestIdsSize:
                return arguments[0] is MapTerm sizeMap ? Term.I(sizeMap.Entries.Count) : throw Failure(ErpcAtoms.BadArgument);
            case ErpcNames.RequestIdsAdd:
                {
                    var (result, request) = RequestId(arguments[0]);
                    MapTerm map = Collection(arguments[2]);
                    if (map.TryGet(request, out _))
                        throw Failure(ErpcAtoms.BadArgument);

                    return Put(map, request, new Cons(result, arguments[1]));
                }
            case ErpcNames.RequestIdsToList:
                {
                    Term list = Nil.Value;
                    foreach (var entry in MapIteration.Entries(Collection(arguments[0]), context.Runtime))
                    {
                        var (result, label) = RequestInfo(entry.Key, entry.Value);
                        list = new Cons(Term.Tuple(new Cons(result, entry.Key), label), list);
                    }

                    return list;
                }
            case ErpcNames.Multicall:
                return await Multicall(context, ExpandFunction(arguments, true));
            case ErpcNames.Multicast:
                return await Multicast(context, ExpandFunction(arguments, false));
            case ErpcNames.CastPublic:
                return await Cast(context, ExpandFunction(arguments, false));
            case ErpcNames.Execute when arguments.Count == 4:
                return await Execute(context, arguments);
            case ErpcNames.Execute:
                return Term.Tuple(ErpcAtoms.Return, await ExecuteLocal(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                ));
            case ErpcNames.Cast:
                return await ExecuteCast(context, arguments);
            case ErpcNames.IsArgumentError:
                return await ArgumentSystemLimit(context, new ErlangException(arguments[0]), arguments[3]) ? ErpcAtoms.True : ErpcAtoms.False;
            case ErpcNames.TrimStack:
                return ErpcStack.Trim(
                    arguments[0],
                    arguments[1],
                    arguments[2],
                    arguments[3],
                    context
                );
            case ErpcNames.CallResult:
                return await ResultByType(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2],
                    arguments[3]
                );
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    private static IReadOnlyList<Term> ExpandFunction(IReadOnlyList<Term> arguments, bool validate)
    {
        if (arguments.Count > 3)
            return arguments;
        if (validate && arguments[1] is not FunctionTerm { Arity: 0 })
            throw Failure(ErpcAtoms.BadArgument);
        Term[] expanded = [arguments[0], Term.A(ProcLibNames.Erlang), Term.A(ErlangBasicBifNames.Apply), Term.List(arguments[1], Nil.Value)];

        return arguments.Count == 3 ? [.. expanded, arguments[2]] : expanded;
    }

    private static void ValidateTarget(
        Term node,
        Term module,
        Term function,
        Term arguments
    )
    {
        if (node is not Atom || module is not Atom || function is not Atom || arguments is not (Cons or Nil))
            throw Failure(ErpcAtoms.BadArgument);
    }

    private static async ValueTask<Term> SpawnRequest(
        ProcessContext context,
        Term node,
        Term module,
        Term function,
        Term arguments,
        Term options
    ) => await context.Runtime.Modules.Call(
        context,
        ProcLibNames.Erlang,
        ProcessSpawnRequestNames.Function,
        node,
        module,
        function,
        arguments,
        options
    );

    private static async ValueTask<Term> SendRequest(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments.Count == 2)
            arguments = ExpandFunction(arguments, true);
        else if (arguments.Count == 4 && arguments[1] is FunctionTerm { Arity: 0 } && arguments[3] is MapTerm)
            arguments = [arguments[0], Term.A(ProcLibNames.Erlang), Term.A(ErlangBasicBifNames.Apply), Term.List(arguments[1], Nil.Value), arguments[2], arguments[3]];
        ValidateTarget(
            arguments[0],
            arguments[1],
            arguments[2],
            arguments[3]
        );
        MapTerm? collection = arguments.Count == 6 ? Collection(arguments[5]) : null;
        ReferenceTerm result = context.Runtime.MakeReference();
        Term request = await SpawnRequest(
            context,
            arguments[0],
            ErpcAtoms.Module,
            ErpcAtoms.Execute,
            Term.List(
                result,
                arguments[1],
                arguments[2],
                arguments[3]
            ),
            Term.List(
                Term.Tuple(Term.A(ProcessSpawnRequestNames.Reply), Term.A(ProcessSpawnRequestNames.ErrorOnly)),
                Term.A(ProcessSpawnOptionNames.Monitor)
            )
        );

        return collection is null ? new Cons(result, request) : Put(collection, request, new Cons(result, arguments[4]));
    }

    private static (ReferenceTerm Result, ReferenceTerm Request) RequestId(Term value)
    {
        if (value is Cons { Head: ReferenceTerm result, Tail: ReferenceTerm request })
            return (result, request);

        throw Failure(ErpcAtoms.BadArgument);
    }

    private static MapTerm Collection(Term value) => value as MapTerm ?? throw Failure(ErpcAtoms.BadArgument);

    private static (ReferenceTerm Result, Term Label) RequestInfo(Term key, Term value)
    {
        if (key is ReferenceTerm && value is Cons { Head: ReferenceTerm result } info)
            return (result, info.Tail);

        throw Failure(ErpcAtoms.BadArgument);
    }

    private static MapTerm Put(MapTerm map, Term key, Term value)
        => new(map.Entries.Where(entry => !entry.Key.Equals(key)).Append(new KeyValuePair<Term, Term>(key, value)));

    private static bool DeleteOption(Term value)
    {
        if (value.Equals(ErpcAtoms.True))
            return true;
        if (value.Equals(ErpcAtoms.False))
            return false;

        throw Failure(ErpcAtoms.BadArgument);
    }

    private static TupleTerm? SelectCollection(Term value, MapTerm map)
    {
        if (value is TupleTerm { Items.Count: >= 2 } tuple && tuple.Items[1] is ReferenceTerm request && map.TryGet(request, out _))
            return Select(value, request);

        return null;
    }

    private static async ValueTask<Term> ReceiveResponse(ProcessContext context, IReadOnlyList<Term> arguments, bool wait)
    {
        if (arguments.Count == 3)
        {
            MapTerm map = Collection(arguments[0]);
            bool delete = DeleteOption(arguments[2]);
            Term timeout = await NormalizeTimeout(
                context,
                arguments[1],
                function: wait ? ErpcNames.WaitResponse : ErpcNames.ReceiveResponse,
                3
            );
            if (map.Entries.Count == 0)
                return Term.A(ErpcNames.NoRequest);
            TupleTerm? response = await context.ReceiveErlangAsync(value => SelectCollection(value, map), timeout);
            if (response is not null)
                return await CollectionResult(
                    context,
                    response,
                    map,
                    wait,
                    delete,
                    wait ? ErpcNames.WaitResponse : ErpcNames.ReceiveResponse
                );
            if (wait)
                return Term.A(ErpcNames.NoResponse);
            foreach (var entry in MapIteration.Entries(map, context.Runtime))
            {
                try
                {
                    _ = RequestInfo(entry.Key, entry.Value);
                }
                catch (ErlangException error)
                {
                    throw ErpcSource.Capture(
                        context,
                        error,
                        ErpcNames.ReceiveResponse,
                        3,
                        ErpcSourceLines.CollectionMalformed
                    );
                }
                if (!await Abandon(context, entry.Key))
                    _ = await context.ReceiveAsync(value => Select(value, (ReferenceTerm)entry.Key), TimeSpan.Zero);
            }

            throw ErpcSource.Capture(
                context,
                Failure(ErpcAtoms.Timeout),
                ErpcNames.ReceiveResponse,
                3,
                ErpcSourceLines.CollectionTimeout
            );
        }
        var (result, request) = RequestId(arguments[0]);
        Term time = await NormalizeTimeout(
            context,
            arguments.Count == 2 ? arguments[1] : wait ? Term.I(0) : ErpcAtoms.Infinity,
            wait ? ErpcNames.WaitResponse : ErpcNames.ReceiveResponse,
            2
        );
        TupleTerm? reply = await context.ReceiveErlangAsync(value => Select(value, request), time);
        if (reply is null)
            return wait ? Term.A(ErpcNames.NoResponse) : await AfterTimeoutAt(
                context,
                request,
                result,
                ErpcNames.ReceiveResponse,
                2
            );
        Term answer = Result(
            context,
            reply,
            result,
            wait ? ErpcNames.WaitResponse : ErpcNames.ReceiveResponse,
            2
        );

        return wait && reply.Items.Count == 5 ? Term.Tuple(Term.A(ErpcNames.Response), answer) : answer;
    }

    private static async ValueTask<Term> CheckResponse(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments.Count == 3)
        {
            MapTerm map = Collection(arguments[1]);
            bool delete = DeleteOption(arguments[2]);
            if (map.Entries.Count == 0)
                return Term.A(ErpcNames.NoRequest);
            TupleTerm? response = SelectCollection(arguments[0], map);

            return response is null ? Term.A(ErpcNames.NoResponse) : await CollectionResult(
                context,
                response,
                map,
                true,
                delete,
                ErpcNames.CheckResponse
            );
        }
        var (result, request) = RequestId(arguments[1]);
        TupleTerm? reply = Select(arguments[0], request);
        if (reply is null)
            return Term.A(ErpcNames.NoResponse);
        Term answer = Result(
            context,
            reply,
            result,
            ErpcNames.CheckResponse,
            2
        );

        return reply.Items.Count == 5 ? Term.Tuple(Term.A(ErpcNames.Response), answer) : answer;
    }

    private static ValueTask<Term> CollectionResult(
        ProcessContext context,
        TupleTerm response,
        MapTerm map,
        bool wrap,
        bool delete,
        string function
    )
    {
        Term request = response.Items[1];
        ReferenceTerm result;
        Term label;
        try
        {
            (result, label) = RequestInfo(request, map.Get(request));
        }
        catch (ErlangException error)
        {
            throw ErpcSource.Capture(
                context,
                error,
                function,
                3,
                ErpcSourceLines.CollectionInvalid
            );
        }
        MapTerm updated = delete ? new MapTerm(map.Entries.Where(entry => !entry.Key.Equals(request))) : map;
        try
        {
            Term value = Result(
                context,
                response,
                result,
                function,
                3
            );

            return ValueTask.FromResult<Term>(Term.Tuple(wrap ? Term.Tuple(Term.A(ErpcNames.Response), value) : value, label, updated));
        }
        catch (ErlangException error)
        {
            throw ErpcSource.Capture(
                context,
                new ErlangException(Term.Tuple(error.Reason, label, updated), error.ExceptionClass),
                function,
                3,
                ErpcSourceLines.CollectionRethrow
            );
        }
    }

    private static async ValueTask<bool> Abandon(ProcessContext context, Term request)
    {
        Term abandoned = await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcessSpawnRequestNames.AbandonFunction,
            request
        );
        if (abandoned.Equals(ErpcAtoms.True))
            return true;
        Term removed = await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ErlangModuleNames.Demonitor,
            request,
            Term.List(Term.A(ErpcNames.Info))
        );

        return removed.Equals(ErpcAtoms.True);
    }

    private static async ValueTask<Term> ResultByType(
        ProcessContext context,
        Term type,
        Term request,
        Term result,
        Term reason
    )
    {
        if (type is Atom { Name: ErpcNames.Timeout })
            return await AfterTimeoutAt(
                context,
                request,
                result,
                ErpcNames.CallResult,
                4
            );
        if (type is Atom { Name: ProcessSpawnRequestNames.DefaultTag })
            throw ErpcSource.Capture(
                context,
                Failure(reason),
                ErpcNames.CallResult,
                4,
                ErpcSourceLines.ResultSpawn
            );
        if (type is Atom { Name: ErpcNames.Down })
            return Result(
                context,
                Term.Tuple(
                    Term.A(ProcessMessageTags.MonitorDown),
                    request,
                    Term.A(ProcessMonitorKinds.Process),
                    Nil.Value,
                    reason
                ),
                result,
                ErpcNames.CallResult,
                4
            );

        using var frame = context.EnterGeneratedFunction(
            ErpcNames.Module,
            ErpcNames.InlinedResult,
            4,
            ErpcSource.Location(ErpcSourceLines.ResultClause),
            new object()
        );

        throw new ErlangException(ErlangErrorReasons.FunctionClause).WithCapturedStack(context.CaptureErrorStack(Term.List(
            type,
            request,
            result,
            reason
        ), null));
    }

    private static async ValueTask<Term> Cast(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        ValidateTarget(
            arguments[0],
            arguments[1],
            arguments[2],
            arguments[3]
        );
        _ = await SpawnRequest(
            context,
            arguments[0],
            ErpcAtoms.Module,
            Term.A(ErpcNames.Cast),
            Term.List(arguments[1], arguments[2], arguments[3]),
            Term.List(Term.Tuple(Term.A(ProcessSpawnRequestNames.Reply), Term.A(ErpcNames.NoReply)))
        );

        return Term.A(ErpcNames.Ok);
    }

    private static async ValueTask<Term> Multicast(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        try
        {
            ValidateTarget(
                ErpcAtoms.Module,
                arguments[1],
                arguments[2],
                arguments[3]
            );
            Term nodes = arguments[0];
            while (nodes is Cons cell)
            {
                await Cast(context, [cell.Head, arguments[1], arguments[2], arguments[3]]);
                nodes = cell.Tail;
            }
            if (nodes is not Nil)
                throw Failure(ErpcAtoms.BadArgument);

            return Term.A(ErpcNames.Ok);
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
        {
            throw Failure(ErpcAtoms.BadArgument);
        }
    }

    private static async ValueTask<Term> ExecuteCast(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        using var scope = context.EnterGeneratedFunction(
            ErpcNames.Module,
            ErpcNames.Cast,
            3,
            ErpcSource.Location(ErpcSourceLines.CastApply),
            new object()
        );
        try
        {
            return await Apply(
                context,
                arguments[0],
                arguments[1],
                arguments[2]
            );
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
        {
            if (await ArgumentSystemLimit(context, error, arguments[2]))
                throw Failure(error.Reason);

            throw new ErlangException(Term.Tuple(ErpcAtoms.Exception, Term.Tuple(error.Reason, Stack(
                error,
                arguments[0],
                arguments[1],
                arguments[2]
            ))));
        }
    }
}
