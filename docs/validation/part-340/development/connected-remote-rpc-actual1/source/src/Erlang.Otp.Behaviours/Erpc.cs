namespace Erlang.Otp;

internal static class Erpc
{
    internal static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 4, 5 })
            registry.Register(
                ErpcNames.Module,
                ErpcNames.Call,
                arity,
                Call
            );
        registry.Register(
            ErpcNames.Module,
            ErpcNames.Execute,
            4,
            Execute
        );
    }

    internal static async ValueTask<Term> Call(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term node = arguments[0], module = arguments[1], function = arguments[2], values = arguments[3];
        Term timeout = arguments.Count == 4 ? ErpcAtoms.Infinity : arguments[4];
        Term alwaysSpawn = ErpcAtoms.False;
        if (timeout is MapTerm options)
        {
            timeout = options.TryGet(ErpcAtoms.TimeoutKey, out var configured) ? configured : ErpcAtoms.Infinity;
            alwaysSpawn = options.TryGet(ErpcAtoms.AlwaysSpawn, out var spawn) ? spawn : ErpcAtoms.False;
        }
        if (node is not Atom || module is not Atom || function is not Atom || values is not (Cons or Nil) ||
            !alwaysSpawn.Equals(ErpcAtoms.True) && !alwaysSpawn.Equals(ErpcAtoms.False))
            throw Failure(ErpcAtoms.BadArgument);
        if (node.Equals(context.Runtime.NodeAtom) && timeout.Equals(ErpcAtoms.Infinity) && alwaysSpawn.Equals(ErpcAtoms.False))
        {
            try
            {
                return await Apply(
                    context,
                    module,
                    function,
                    values
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit)
            {
                throw new ErlangException(Term.Tuple(ErpcAtoms.Exception, error.Reason), ErlangExceptionClasses.Exit);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
            {
                if (await ArgumentSystemLimit(context, error, values))
                    throw Failure(error.Reason);
                throw new ErlangException(Term.Tuple(ErpcAtoms.Exception, error.Reason, Stack(
                    error,
                    module,
                    function,
                    values
                )));
            }
        }
        timeout = await ErpcTimeout.Normalize(context, timeout);
        ReferenceTerm result = context.Runtime.MakeReference();
        var request = (ReferenceTerm)await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcessSpawnRequestNames.Function,
            node,
            ErpcAtoms.Module,
            ErpcAtoms.Execute,
            Term.List(
                result,
                module,
                function,
                values
            ),
            Term.List(
                Term.Tuple(Term.A(ProcessSpawnRequestNames.Reply), Term.A(ProcessSpawnRequestNames.ErrorOnly)),
                Term.A(ProcessSpawnOptionNames.Monitor)
            )
        );

        return await WaitResult(
            context,
            request,
            result,
            timeout
        );
    }

    private static async ValueTask<Term> WaitResult(
        ProcessContext context,
        ReferenceTerm request,
        ReferenceTerm result,
        Term timeout
    )
    {
        TupleTerm? response = await context.ReceiveErlangAsync(value => Select(value, request), timeout);

        return response is null ? await AfterTimeout(context, request, result) : Result(response, result);
    }

    private static async ValueTask<Term> AfterTimeout(ProcessContext context, ReferenceTerm request, ReferenceTerm result)
    {
        Term abandoned = await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcessSpawnRequestNames.AbandonFunction,
            request
        );
        if (abandoned.Equals(ErpcAtoms.True))
            throw Failure(ErpcAtoms.Timeout);
        Term removed = await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ErlangModuleNames.Demonitor,
            request,
            Term.List(Term.A(ErpcNames.Info))
        );
        if (removed.Equals(ErpcAtoms.True))
            throw Failure(ErpcAtoms.Timeout);
        TupleTerm? response = await context.ReceiveAsync(value => Select(value, request), TimeSpan.Zero);
        if (response is null)
            throw Failure(ErpcAtoms.BadArgument);

        return Result(response, result);
    }

    private static TupleTerm? Select(Term value, ReferenceTerm reference) => value switch
    {
        TupleTerm { Items: [Atom { Name: ProcessSpawnRequestNames.DefaultTag }, var request, Atom { Name: ProcessSpawnRequestNames.Error }, _] } tuple
            when request.Equals(reference) => tuple,
        TupleTerm { Items: [Atom { Name: ProcessMessageTags.MonitorDown }, var request, Atom { Name: ProcessMonitorKinds.Process }, _, _] } tuple
            when request.Equals(reference) => tuple,
        _ => null
    };

    private static Term Result(TupleTerm response, ReferenceTerm result)
    {
        if (response.Items.Count == 4)
            throw Failure(response.Items[3]);
        Term reason = response.Items[4];
        if (reason is TupleTerm { Items: [var reference, Atom kind, var value] } && reference.Equals(result))
        {
            if (kind.Name == ErpcNames.Return)
                return value;
            if (kind.Name == ErpcNames.Throw)
                throw new ErlangException(value, ErlangExceptionClasses.Throw);
            if (kind.Name == ErpcNames.Exit)
                throw new ErlangException(Term.Tuple(ErpcAtoms.Exception, value), ErlangExceptionClasses.Exit);
            if (kind.Name == ErpcNames.Error && value is TupleTerm { Items: [Atom { Name: ErpcNames.Module }, _] })
                throw new ErlangException(value);
        }
        if (reason is TupleTerm { Items: [var correlation, Atom { Name: ErpcNames.Error }, var error, var stack] } && correlation.Equals(result))
            throw new ErlangException(Term.Tuple(ErpcAtoms.Exception, error, stack));
        if (reason.Equals(ErpcAtoms.NoConnection))
            throw Failure(ErpcAtoms.NoConnection);

        throw new ErlangException(Term.Tuple(ErpcAtoms.Signal, reason), ErlangExceptionClasses.Exit);
    }

    private static async ValueTask<Term> Execute(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term reference = arguments[0], module = arguments[1], function = arguments[2], values = arguments[3];
        Term reply;
        try
        {
            reply = Term.Tuple(reference, ErpcAtoms.Return, await Apply(
                context,
                module,
                function,
                values
            ));
        }
        catch (ErlangException error)
        {
            reply = error.ExceptionClass switch
            {
                ErlangExceptionClasses.Throw => Term.Tuple(reference, ErpcAtoms.Throw, error.Reason),
                ErlangExceptionClasses.Exit => Term.Tuple(reference, ErpcAtoms.Exit, error.Reason),
                _ => await ArgumentSystemLimit(context, error, values)
                    ? Term.Tuple(reference, ErpcAtoms.Error, Term.Tuple(ErpcAtoms.Module, error.Reason))
                    : Term.Tuple(
                        reference,
                        ErpcAtoms.Error,
                        error.Reason,
                        Stack(
                            error,
                            module,
                            function,
                            values
                        )
                    )
            };
        }
        throw new ErlangException(reply, ErlangExceptionClasses.Exit);
    }

    private static ValueTask<Term> Apply(
        ProcessContext context,
        Term module,
        Term function,
        Term values
    )
        => ErlangApplyDispatch.Invoke(context, [module, function, values]);

    private static async ValueTask<bool> ArgumentSystemLimit(ProcessContext context, ErlangException error, Term arguments)
    {
        if (!error.Reason.Equals(ErpcAtoms.SystemLimit))
            return false;
        try
        {
            await Apply(
                context,
                ErpcAtoms.Module,
                Term.A(ErpcNames.ArgumentProbe),
                arguments
            );

            return false;
        }
        catch (ErlangException probe)
        {
            return probe.ExceptionClass == ErlangExceptionClasses.Error && probe.Reason.Equals(ErpcAtoms.SystemLimit);
        }
    }

    private static Term Stack(
        ErlangException error,
        Term module,
        Term function,
        Term arguments
    )
    {
        if (error.StackTraceTerm is Nil && !error.ExplicitStackTrace)
            return Term.List(Term.Tuple(
                module,
                function,
                arguments,
                Nil.Value
            ));

        return ErpcStack.Trim(
            error.StackTraceTerm,
            module,
            function,
            arguments
        );
    }

    private static ErlangException Failure(Term reason) => new(Term.Tuple(ErpcAtoms.Module, reason));
}
