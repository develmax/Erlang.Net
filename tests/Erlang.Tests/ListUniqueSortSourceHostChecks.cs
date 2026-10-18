namespace Erlang.Tests;

internal static class ListUniqueSortSourceHostChecks
{
    internal static async Task Identity()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term one = Term.I(1);
        Term two = Term.I(2);
        Term three = Term.I(3);
        foreach (Term list in new[] { Nil.Value, Term.List(one), Term.List(one, two), Term.List(one, two, three) })
            Require(ReferenceEquals(await Sort(actor, runtime, list), list));
        Term first = new FloatTerm(1);
        Term equal = Term.I(1);
        Term result = await Sort(actor, runtime, Term.List(first, equal, new FloatTerm(1)));
        Require(result is Cons { Tail: Nil } selected && ReferenceEquals(selected.Head, first));
        Term large = new FloatTerm(2);
        Term small = Term.I(0);
        Term middle = Term.I(1);
        result = await Sort(actor, runtime, Term.List(
            large,
            small,
            Term.I(2),
            middle,
            new FloatTerm(0)
        ));
        Term[] items = Cons.Items(result).ToArray();
        Require(items.Length == 3 && ReferenceEquals(items[0], small) && ReferenceEquals(items[1], middle) && ReferenceEquals(items[2], large));
        Term duplicate = Term.I(2);
        result = await actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            ListUniqueSortSourceHostNames.Lists,
            ListUniqueSortSourceHostNames.Sort,
            Term.List(large, middle, duplicate)
        ).AsTask());
        items = Cons.Items(result).ToArray();
        Require(items.Length == 3 && ReferenceEquals(items[0], middle) && ReferenceEquals(items[1], large) && ReferenceEquals(items[2], duplicate));
    }

    internal static async Task Malformed()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term tail = Term.A(ListUniqueSortSourceHostNames.Tail);
        Term[][] prefixes = [[Term.I(1)], [Term.I(1), Term.I(1), Term.I(1)], [Term.I(1), Term.I(3)], [Term.I(3), Term.I(1)], [Term.I(1), Term.I(3), Term.I(0), Term.I(-1)], [Term.I(3), Term.I(1), Term.I(4), Term.I(5)]];
        Term[] inputs = prefixes.Select(prefix => Cons.From(prefix, tail)).ToArray();
        Term[] expectedArguments =
        [
            Term.List(inputs[0]),
            Term.List(Term.I(1), tail),
            Term.List(
                Term.I(1),
                Term.I(3),
                tail,
                Nil.Value,
                Nil.Value
            ),
            Term.List(
                Term.I(3),
                Term.I(1),
                tail,
                Nil.Value,
                Nil.Value
            ),
            Term.List(
                Term.I(1),
                Term.I(3),
                tail,
                Term.List(Term.I(0)),
                Nil.Value,
                Term.I(-1)
            ),
            Term.List(
                Term.I(3),
                Term.I(1),
                tail,
                Term.List(Term.I(4)),
                Nil.Value,
                Term.I(5)
            )
        ];
        for (int index = 0; index < inputs.Length; index++)
        {
            bool failed = false;
            try
            {
                await Sort(actor, runtime, inputs[index]);
            }
            catch (ErlangException error)
            {
                failed = true;
                Require(error.Reason.Equals(Term.A(ListUniqueSortSourceHostNames.FunctionClause)));
                Term expected = Term.Tuple(
                    Term.A(ListUniqueSortSourceHostNames.Lists),
                    Term.A(ListUniqueSortSourceHostNames.ErrorFunctions[index]),
                    expectedArguments[index],
                    Term.List(
                        Term.Tuple(Term.A(ListUniqueSortSourceHostNames.File), Term.String(ListUniqueSortSourceHostNames.Source)),
                        Term.Tuple(Term.A(ListUniqueSortSourceHostNames.Line), Term.I(ListUniqueSortSourceHostNames.ErrorLines[index]))
                    )
                );
                Require(Cons.Items(error.StackTraceTerm).First().Equals(expected));
            }
            Require(failed);
        }
    }

    internal static async Task Deep()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term input = Nil.Value;
        for (int index = 0; index < ListUniqueSortSourceHostNames.Cells; index++)
            input = new Cons(Term.I(index * ListUniqueSortSourceHostNames.Step % ListUniqueSortSourceHostNames.Modulus), input);
        Term result = await Sort(actor, runtime, input);
        Term[] ordered = Cons.Items(result).ToArray();
        Require(ordered.Length == ListUniqueSortSourceHostNames.Modulus);
        for (int index = 0; index < ordered.Length; index++)
            Require(ordered[index].Equals(Term.I(index)));
        Term tail = Term.A(ListUniqueSortSourceHostNames.Tail);
        input = tail;
        Term first = new FloatTerm(1);
        for (int index = 0; index < ListUniqueSortSourceHostNames.Cells; index++)
            input = new Cons(first, input);
        bool failed = false;
        try
        {
            await Sort(actor, runtime, input);
        }
        catch (ErlangException error)
        {
            failed = true;
            Require(error.Reason.Equals(Term.A(ListUniqueSortSourceHostNames.FunctionClause)));
            TupleTerm frame = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
            Require(frame.Items[1].Equals(Term.A(ListUniqueSortSourceHostNames.UsortHelper)) && frame.Items[2].Equals(Term.List(first, tail)));
        }
        Require(failed);
    }

    internal static async Task DynamicReverse()
    {
        await using var runtime = new ProcessRuntime();
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        var queue = new ListLengthYieldContext();
        var entered = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failed = new TaskCompletionSource<ErlangException>(TaskCreationOptions.RunContinuationsAsynchronously);
        Term input = Nil.Value;
        for (int index = ListUniqueSortSourceHostNames.Cells - 1; index >= 0; index--)
            input = new Cons(Term.I(index), input);
        var process = runtime.Spawn(async context =>
        {
            Task<Term> operation;
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(queue);
            try
            {
                operation = runtime.Modules.Call(
                    context,
                    ListUniqueSortSourceHostNames.Lists,
                    ListUniqueSortSourceHostNames.UniqueSort,
                    input
                ).AsTask();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
            entered.TrySetResult(operation);
            try
            {
                return await operation;
            }
            catch (ErlangException error)
            {
                failed.TrySetResult(error);

                return Nil.Value;
            }
        });
        Task<Term> pending = await entered.Task.WaitAsync(ListUniqueSortSourceHostNames.Deadline);
        try
        {
            Require(!pending.IsCompleted);
            int reverseCalls = 0;
            IReadOnlyList<Term>? selectedReverseArguments = null;
            Term marker = Term.A(ListUniqueSortSourceHostNames.ReplacementFailure);
            await observer.Invoke(_ =>
            {
                runtime.Modules.Apply(registry =>
                {
                    registry.DeleteModule(ListUniqueSortSourceHostNames.Lists);
                    registry.Register(
                        ListUniqueSortSourceHostNames.Lists,
                        ListUniqueSortSourceHostNames.Reverse,
                        2,
                        (_, arguments) =>
                    {
                        reverseCalls++;
                        selectedReverseArguments = arguments.ToArray();
                        Require(arguments[0] is Cons && arguments[1] is Nil);

                        throw new ErlangException(marker);
                    }
                    );
                });

                return true;
            });
            await queue.ResumeUntil(pending);
            ErlangException failure = await failed.Task.WaitAsync(ListUniqueSortSourceHostNames.Deadline);
            if (reverseCalls != 1 || !ReferenceEquals(failure.Reason, marker))
                throw new InvalidOperationException(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    ListUniqueSortSourceHostNames.DynamicReverseFailure,
                    reverseCalls,
                    failure.ExceptionClass,
                    failure.Reason is Atom atom ? atom.Name : failure.Reason.GetType().Name,
                    failure.Reason.Equals(marker),
                    selectedReverseArguments?.Count,
                    selectedReverseArguments is { Count: > 0 } ? selectedReverseArguments[0].GetType().Name : null,
                    selectedReverseArguments is { Count: > 1 } ? selectedReverseArguments[1].GetType().Name : null
                ));
            Require((await process.Completion.WaitAsync(ListUniqueSortSourceHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        }
        finally
        {
            if (!pending.IsCompleted)
            {
                await observer.Invoke(context =>
                {
                    runtime.Exit(context, process.Pid, Term.A(ListUniqueSortSourceHostNames.Kill));

                    return true;
                });
                await queue.ResumeUntil(pending);
            }
            await process.Completion.WaitAsync(ListUniqueSortSourceHostNames.Deadline);
        }
    }

    private static Task<Term> Sort(RuntimeNodeFoundationHostActor actor, ProcessRuntime runtime, Term list)
        => actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            ListUniqueSortSourceHostNames.Lists,
            ListUniqueSortSourceHostNames.UniqueSort,
            list
        ).AsTask());

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ListUniqueSortSourceHostNames.Failure);
    }
}
