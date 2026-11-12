// Derived from Erlang/OTP 29.1.1 erpc.erl (Apache-2.0). Copyright Ericsson AB 2020-2025.
namespace Erlang.Otp;

internal static partial class Erpc
{
    internal static void Register(ModuleRegistry registry)
    {
        foreach (var export in ErpcLibrary.Exports)
        {
            string function = export.Name;
            registry.RegisterReviewedManagedLibrary789(
                ErpcNames.Module,
                function,
                export.Arity,
                (context, arguments) => Invoke(context, function, arguments)
            );
        }
    }

    internal static async ValueTask<Term> Call(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term node = arguments[0], module = arguments[1], function = arguments[2], values = arguments[3];
        Term timeout = arguments.Count == 4 ? ErpcAtoms.Infinity : arguments[4];
        Term alwaysSpawn = ErpcAtoms.False;
        if (timeout is MapTerm options)
        {
            timeout = options.TryGet(ErpcAtoms.TimeoutKey, out var configured) ? configured! : ErpcAtoms.Infinity;
            alwaysSpawn = options.TryGet(ErpcAtoms.AlwaysSpawn, out var spawn) ? spawn! : ErpcAtoms.False;
        }
        if (node is not Atom || module is not Atom || function is not Atom || values is not (Cons or Nil) ||
            !alwaysSpawn.Equals(ErpcAtoms.True) && !alwaysSpawn.Equals(ErpcAtoms.False))
            throw Failure(ErpcAtoms.BadArgument);
        if (node.Equals(context.Runtime.NodeAtom) && timeout.Equals(ErpcAtoms.Infinity) && alwaysSpawn.Equals(ErpcAtoms.False))
        {
            try
            {
                using var frame = context.EnterGeneratedFunction(
                    ErpcNames.Module,
                    ErpcNames.Call,
                    5,
                    ErpcSource.Location(ErpcSourceLines.CallLocal),
                    new object()
                );

                return await ExecuteLocal(
                    context,
                    module,
                    function,
                    values
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit)
            {
                throw ErpcSource.Capture(
                    context,
                    new ErlangException(Term.Tuple(ErpcAtoms.Exception, error.Reason), ErlangExceptionClasses.Exit),
                    ErpcNames.Call,
                    5,
                    ErpcSourceLines.CallLocalExit
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
            {
                if (await ArgumentSystemLimit(context, error, values))
                    throw ErpcSource.Capture(
                        context,
                        Failure(error.Reason),
                        ErpcNames.Call,
                        5,
                        ErpcSourceLines.CallLocalArgument
                    );
                throw ErpcSource.Capture(
                    context,
                    new ErlangException(Term.Tuple(ErpcAtoms.Exception, error.Reason, Stack(
                        error,
                        module,
                        function,
                        values
                    ))),
                    ErpcNames.Call,
                    5,
                    ErpcSourceLines.CallLocalError
                );
            }
        }
        timeout = await NormalizeTimeout(
            context,
            timeout,
            ErpcNames.Call,
            5
        );
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

        return response is null ? await AfterTimeout(context, request, result) : Result(context, response, result);
    }

    private static ValueTask<Term> AfterTimeout(ProcessContext context, ReferenceTerm request, ReferenceTerm result)
        => AfterTimeoutAt(
            context,
            request,
            result,
            ErpcNames.Call,
            5
        );

    private static async ValueTask<Term> AfterTimeoutAt(
        ProcessContext context,
        Term request,
        Term result,
        string function,
        int arity
    )
    {
        Term abandoned = await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcessSpawnRequestNames.AbandonFunction,
            request
        );
        if (abandoned.Equals(ErpcAtoms.True))
            throw ErpcSource.Capture(
                context,
                Failure(ErpcAtoms.Timeout),
                function,
                arity,
                ErpcSourceLines.ResultTimeout
            );
        Term removed = await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ErlangModuleNames.Demonitor,
            request,
            Term.List(Term.A(ErpcNames.Info))
        );
        if (removed.Equals(ErpcAtoms.True))
            throw ErpcSource.Capture(
                context,
                Failure(ErpcAtoms.Timeout),
                function,
                arity,
                ErpcSourceLines.ResultTimeout
            );
        TupleTerm? response = await context.ReceiveAsync(value => Select(value, request), TimeSpan.Zero);
        if (response is null)
            throw ErpcSource.Capture(
                context,
                Failure(ErpcAtoms.BadArgument),
                function,
                arity,
                ErpcSourceLines.ResultInvalid
            );

        return Result(
            context,
            response,
            result,
            function,
            arity
        );
    }

    private static TupleTerm? Select(Term value, Term reference) => value switch
    {
        TupleTerm { Items: [Atom { Name: ProcessSpawnRequestNames.DefaultTag }, var request, Atom { Name: ProcessSpawnRequestNames.Error }, _] } tuple
            when request.Equals(reference) => tuple,
        TupleTerm { Items: [Atom { Name: ProcessMessageTags.MonitorDown }, var request, Atom { Name: ProcessMonitorKinds.Process }, _, _] } tuple
            when request.Equals(reference) => tuple,
        _ => null
    };

    private static Term Result(
        ProcessContext context,
        TupleTerm response,
        Term result,
        string function = ErpcNames.Call,
        int arity = 5
    )
    {
        ErlangException Capture(ErlangException error, int line) => ErpcSource.Capture(
            context,
            error,
            function,
            arity,
            line
        );
        if (response.Items.Count == 4)
            throw Capture(Failure(response.Items[3]), ErpcSourceLines.ResultSpawn);
        Term reason = response.Items[4];
        if (reason is TupleTerm { Items: [var reference, Atom kind, var value] } && reference.Equals(result))
        {
            if (kind.Name == ErpcNames.Return)
                return value;
            if (kind.Name == ErpcNames.Throw)
                throw Capture(new ErlangException(value, ErlangExceptionClasses.Throw), ErpcSourceLines.ResultThrow);
            if (kind.Name == ErpcNames.Exit)
                throw Capture(new ErlangException(Term.Tuple(ErpcAtoms.Exception, value), ErlangExceptionClasses.Exit), ErpcSourceLines.ResultExit);
            if (kind.Name == ErpcNames.Error && value is TupleTerm { Items: [Atom { Name: ErpcNames.Module }, _] })
                throw Capture(new ErlangException(value), ErpcSourceLines.ResultArgument);
        }
        if (reason is TupleTerm { Items: [var correlation, Atom { Name: ErpcNames.Error }, var error, var stack] } && correlation.Equals(result))
            throw Capture(new ErlangException(Term.Tuple(ErpcAtoms.Exception, error, stack)), ErpcSourceLines.ResultError);
        if (reason.Equals(ErpcAtoms.NoConnection))
            throw Capture(Failure(ErpcAtoms.NoConnection), ErpcSourceLines.ResultConnection);

        throw Capture(new ErlangException(Term.Tuple(ErpcAtoms.Signal, reason), ErlangExceptionClasses.Exit), ErpcSourceLines.ResultSignal);
    }

    private static async ValueTask<Term> Execute(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        using var scope = context.EnterSourceWrapper(ErpcNames.Module, ErpcNames.Execute, 4);
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

    private static async ValueTask<Term> ExecuteLocal(
        ProcessContext context,
        Term module,
        Term function,
        Term values
    )
    {
        using var scope = context.EnterGeneratedFunction(
            ErpcNames.Module,
            ErpcNames.Execute,
            ErpcNames.ExecuteLocalArity,
            ErpcSource.Location(ErpcSourceLines.ExecuteLocal),
            new object()
        );

        return await Apply(
            context,
            module,
            function,
            values
        );
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
        if (error.StackTraceTerm is Nil && !error.ExplicitStackTrace && !error.AutomaticStackTraceCaptured)
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
