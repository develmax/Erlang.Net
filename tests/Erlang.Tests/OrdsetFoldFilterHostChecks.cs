namespace Erlang.Tests;

internal static class OrdsetFoldFilterHostChecks
{
    internal static async Task LateFailures()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term failure = Term.A(OrdsetFoldFilterHostNames.LateFailure);
        int visits = 0;
        var fold = new FunctionTerm(2, (_, arguments) =>
        {
            visits++;
            if (visits == 3)
                throw new ErlangException(failure);

            return ValueTask.FromResult(arguments[0]);
        });
        ErlangException error = await Catch(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Fold,
            fold,
            Nil.Value,
            Term.List(Term.I(1), Term.I(2), Term.I(3))
        );
        Require(visits == 3 && ReferenceEquals(error.Reason, failure));
        SourceFrame(
            error,
            OrdsetFoldFilterHostNames.FoldHelper,
            3,
            OrdsetFoldFilterHostNames.FoldHelperCallLine
        );
        foreach (bool badReturn in new[] { false, true })
        {
            visits = 0;
            var filter = new FunctionTerm(
                1,
                (_, _) =>
            {
                visits++;
                if (visits == 3)
                {
                    if (badReturn)
                        return ValueTask.FromResult(failure);
                    throw new ErlangException(failure);
                }

                return ValueTask.FromResult<Term>(Term.A(visits == 1 ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
            }
            );
            error = await Catch(
                actor,
                runtime,
                OrdsetFoldFilterHostNames.Filter,
                filter,
                Term.List(Term.I(1), Term.I(2), Term.I(3))
            );
            Require(visits == 3);
            Require(error.Reason.Equals(badReturn ? Term.Tuple(Term.A(OrdsetFoldFilterHostNames.BadFilter), failure) : failure));
            SourceFrame(
                error,
                OrdsetFoldFilterHostNames.Comprehension,
                2,
                OrdsetFoldFilterHostNames.FilterCallLine,
                2
            );
        }
        visits = 0;
        var mixed = new FunctionTerm(1, (_, _) => ValueTask.FromResult<Term>(Term.A(++visits == 1 ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)));
        Term tail = Term.A(OrdsetFoldFilterHostNames.Tail);
        error = await Catch(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Filter,
            mixed,
            new Cons(Term.I(1), new Cons(Term.I(2), tail))
        );
        Require(visits == 2 && error.Reason.Equals(Term.Tuple(Term.A(OrdsetFoldFilterHostNames.BadGenerator), tail)));
        SourceFrame(
            error,
            OrdsetFoldFilterHostNames.Comprehension,
            2,
            OrdsetFoldFilterHostNames.FilterCallLine,
            2
        );
    }

    internal static async Task IdentityAsync()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term payload = new MapTerm(new Dictionary<Term, Term> { [Term.I(2)] = Term.Tuple(Term.I(3)) });
        Term callbackPayload = new FunctionTerm(0, (_, _) => ValueTask.FromResult(payload));
        Term[] elements = [payload, callbackPayload, payload];
        Term list = Cons.From(elements);
        Term initial = Term.Tuple(callbackPayload, payload);
        ProcessContext expectedContext = await actor.Ready;
        int visits = 0;
        Term previous = initial;
        var fold = new FunctionTerm(
            2,
            async (context, arguments) =>
        {
            Require(ReferenceEquals(context, expectedContext));
            Require(ReferenceEquals(arguments[0], elements[visits]));
            Require(ReferenceEquals(arguments[1], previous));
            await Task.Yield();
            previous = arguments[0];
            visits++;

            return previous;
        }
        );
        Term result = await Call(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Fold,
            fold,
            initial,
            list
        );
        Require(visits == elements.Length && ReferenceEquals(result, previous));
        result = await Call(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Fold,
            fold,
            initial,
            Nil.Value
        );
        Require(ReferenceEquals(result, initial) && visits == elements.Length);
        visits = 0;
        var filter = new FunctionTerm(
            1,
            async (context, arguments) =>
        {
            Require(ReferenceEquals(context, expectedContext));
            Require(ReferenceEquals(arguments[0], elements[visits++]));
            await Task.Yield();

            return Term.A(ErlangBooleanAtoms.True);
        }
        );
        result = await Call(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Filter,
            filter,
            list
        );
        Term[] retained = Cons.Items(result).ToArray();
        Require(visits == elements.Length && retained.Length == elements.Length);
        for (int index = 0; index < elements.Length; index++)
            Require(ReferenceEquals(elements[index], retained[index]));
    }

    internal static async Task Deep()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term payload = Term.Tuple(Term.I(1));
        Term list = Nil.Value;
        for (int index = 0; index < OrdsetFoldFilterHostNames.Cells; index++)
            list = new Cons(payload, list);
        int visits = 0;
        var fold = new FunctionTerm(2, (_, arguments) =>
        {
            visits++;

            return ValueTask.FromResult(arguments[1]);
        });
        Term result = await Call(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Fold,
            fold,
            payload,
            list
        );
        Require(visits == OrdsetFoldFilterHostNames.Cells && ReferenceEquals(result, payload));
        visits = 0;
        var filter = new FunctionTerm(1, (_, _) =>
        {
            visits++;

            return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.True));
        });
        result = await Call(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Filter,
            filter,
            list
        );
        int count = 0;
        while (result is Cons cell)
        {
            Require(ReferenceEquals(cell.Head, payload));
            count++;
            result = cell.Tail;
        }
        Require(result is Nil && count == OrdsetFoldFilterHostNames.Cells && visits == count);
        Term tail = Term.A(OrdsetFoldFilterHostNames.Tail);
        Term improper = tail;
        for (int index = 0; index < OrdsetFoldFilterHostNames.Cells; index++)
            improper = new Cons(payload, improper);
        visits = 0;
        ErlangException error = await Catch(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Fold,
            fold,
            payload,
            improper
        );
        Require(visits == OrdsetFoldFilterHostNames.Cells && error.Reason.Equals(Term.A(OrdsetFoldFilterHostNames.FunctionClause)));
        TupleTerm top = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
        Term[] residual = Cons.Items(top.Items[2]).ToArray();
        Require(residual.Length == 3 && ReferenceEquals(residual[0], fold) && ReferenceEquals(residual[1], payload) && ReferenceEquals(residual[2], tail));
        visits = 0;
        error = await Catch(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Filter,
            filter,
            improper
        );
        Require(visits == OrdsetFoldFilterHostNames.Cells && error.Reason.Equals(Term.Tuple(Term.A(OrdsetFoldFilterHostNames.BadGenerator), tail)));
        SourceFrame(
            error,
            OrdsetFoldFilterHostNames.Comprehension,
            2,
            OrdsetFoldFilterHostNames.FilterCallLine,
            2
        );
    }

    internal static async Task Cancellation()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (string operation in new[] { OrdsetFoldFilterHostNames.Fold, OrdsetFoldFilterHostNames.Filter })
        {
            using var cancellation = new CancellationTokenSource();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int visits = 0;
            var callback = new FunctionTerm(
                operation == OrdsetFoldFilterHostNames.Fold ? 2 : 1,
                async (_, arguments) =>
            {
                if (++visits == 1)
                    return operation == OrdsetFoldFilterHostNames.Fold ? arguments[1] : Term.A(ErlangBooleanAtoms.True);
                entered.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.Infinite, cancellation.Token);

                    return Nil.Value;
                }
                finally
                {
                    exited.TrySetResult();
                }
            }
            );
            Term list = Term.List(Term.I(1), Term.I(2), Term.I(3));
            Task<Term> pending = operation == OrdsetFoldFilterHostNames.Fold
                ? Call(
                    actor,
                    runtime,
                    operation,
                    callback,
                    Nil.Value,
                    list
                )
                : Call(
                    actor,
                    runtime,
                    operation,
                    callback,
                    list
                );
            try
            {
                await entered.Task.WaitAsync(OrdsetFoldFilterHostNames.Deadline);
                Require(!pending.IsCompleted);
                cancellation.Cancel();
                bool canceled = false;
                try
                {
                    await pending;
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                Require(canceled && visits == 2);
                await exited.Task.WaitAsync(OrdsetFoldFilterHostNames.Deadline);
            }
            finally
            {
                cancellation.Cancel();
            }
            Term reason = Term.A(OrdsetFoldFilterHostNames.FollowupFailure);
            ErlangException error = await Catch(
                actor,
                runtime,
                OrdsetFoldFilterHostNames.Erlang,
                OrdsetFoldFilterHostNames.Error,
                reason
            );
            Require(ReferenceEquals(error.Reason, reason));
            Require(!Cons.Items(error.StackTraceTerm).OfType<TupleTerm>().Any(frame => frame.Items.Count == 4 && (frame.Items[0].Equals(Term.A(OrdsetFoldFilterHostNames.Lists)) || frame.Items[0].Equals(Term.A(OrdsetFoldFilterHostNames.Module)))));
        }
    }

    private static Task<Term> Call(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        string operation,
        params Term[] arguments
    )
        => actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            OrdsetFoldFilterHostNames.Module,
            operation,
            arguments
        ).AsTask());

    private static Task<ErlangException> Catch(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        string operation,
        params Term[] arguments
    )
        => Catch(
            actor,
            runtime,
            OrdsetFoldFilterHostNames.Module,
            operation,
            arguments
        );

    private static async Task<ErlangException> Catch(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        string module,
        string operation,
        params Term[] arguments
    )
    {
        try
        {
            await actor.InvokeAsync(context => runtime.Modules.Call(
                context,
                module,
                operation,
                arguments
            ).AsTask());
        }
        catch (ErlangException error)
        {
            return error;
        }

        throw new InvalidOperationException(OrdsetFoldFilterHostNames.Failure);
    }

    private static void SourceFrame(
        ErlangException error,
        string function,
        int arity,
        int line,
        int expectedFrames = 1
    )
    {
        Term expected = Term.Tuple(
            Term.A(OrdsetFoldFilterHostNames.Lists),
            Term.A(function),
            Term.I(arity),
            Term.List(
                Term.Tuple(Term.A(OrdsetFoldFilterHostNames.File), Term.String(OrdsetFoldFilterHostNames.Source)),
                Term.Tuple(Term.A(OrdsetFoldFilterHostNames.Line), Term.I(line))
            )
        );
        Require(Cons.Items(error.StackTraceTerm).Count(frame => frame.Equals(expected)) == expectedFrames);
        Require(!Cons.Items(error.StackTraceTerm).OfType<TupleTerm>().Any(frame => frame.Items.Count == 4 && frame.Items[0].Equals(Term.A(OrdsetFoldFilterHostNames.Module))));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(OrdsetFoldFilterHostNames.Failure);
    }
}
