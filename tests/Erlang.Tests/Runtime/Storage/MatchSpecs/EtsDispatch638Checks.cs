using System.Runtime.ExceptionServices;
using static Erlang.Tests.EtsDispatch638Access;
using static Erlang.Tests.EtsDispatch638Names;

namespace Erlang.Tests;

internal static class EtsDispatch638Checks
{
    internal static Task RunTrap() => Exercise(false, false, false);

    internal static Task HashTrap() => Exercise(true, false, false);

    internal static Task TreeTrap() => Exercise(true, true, false);

    internal static Task RunCancellation() => Exercise(false, false, true);

    internal static Task TreeCancellation() => Exercise(true, true, true);

    private static async Task Exercise(bool cursor, bool tree, bool cancel)
    {
        var runtime = new ProcessRuntime(1);
        RuntimeNodeFoundationHostActor? actor = null;
        object? native = null;
        Task<Term>? operation = null;
        Task? retirement = null, stopping = null;
        ProcessContext? context = null;
        var errors = new List<Exception>();
        using var fence = new EtsDispatch638Fence();
        try
        {
            Install(runtime);
            actor = new(runtime);
            context = await actor.Ready.WaitAsync(Deadline);
            var published = await actor.Invoke(PublishNative);
            native = published.Native;
            Term argument;
            int expected;
            string function;
            Term[] arguments;
            if (cursor)
            {
                function = EtsCursors420Names.Select;
                expected = CursorRows;
                Term table = await actor.InvokeAsync(async owner => await runtime.Modules.Call(
                    owner,
                    EtsCursors420Names.Module,
                    EtsCursors420Names.New,
                    Term.A(EtsCursors420Names.Name),
                    Term.List(Term.A(tree ? EtsCursors420Names.Ordered : EtsCursors420Names.Set))
                ));
                _ = await actor.InvokeAsync(async owner => await runtime.Modules.Call(
                    owner,
                    EtsCursors420Names.Module,
                    EtsCursors420Names.Insert,
                    table,
                    Cons.From(Enumerable.Range(1, CursorRows).Select(index => Term.Tuple(Term.I(index), Term.I(index))))
                ));
                argument = tree
                    ? Term.Tuple(
                        table,
                        Term.I(0),
                        Nil.Value,
                        Term.I(0),
                        published.Reference,
                        Nil.Value,
                        Term.I(1),
                        Term.I(0)
                    )
                    : Term.Tuple(
                        table,
                        Term.I(0),
                        Term.I(0),
                        published.Reference,
                        Nil.Value,
                        Term.I(0)
                    );
                arguments = [argument];
            }
            else
            {
                function = EtsCompiled419Names.Run;
                expected = RunRows;
                argument = Cons.From(Enumerable.Range(1, RunRows).Select(index => Term.I(index)));
                arguments = [argument, published.Reference, Nil.Value];
            }
            ErlangFunction body = EtsCompiled419Access.Body(runtime, function, arguments.Length);
            _ = await actor.Invoke(owner =>
            {
                SynchronizationContext? previous = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(fence);
                try
                {
                    // Real registered body runs synchronously until its actual
                    // budget Task.Yield posts the withheld continuation.
                    operation = body(owner, arguments).AsTask();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(previous);
                }

                return true;
            });
            await fence.Posted.WaitAsync(Deadline);
            Require(operation is not null && !operation.IsCompleted);
            retirement = (Task)Call(native, Close)!;
            Require(!retirement.IsCompleted && !PhysicalFreed(native));
            if (cancel)
            {
                stopping = runtime.DisposeAsync().AsTask();
                using var deadline = new CancellationTokenSource(Deadline);
                while (!context.Cancellation.IsCancellationRequested)
                    await Task.Delay(PollMilliseconds, deadline.Token);
                Require(!stopping.IsCompleted && !retirement.IsCompleted && !PhysicalFreed(native));
            }
            fence.Release();
            if (cancel)
            {
                bool observed = false;
                try
                {
                    _ = await operation!.WaitAsync(Deadline);
                }
                catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
                {
                    observed = true;
                }
                Require(observed);
                await stopping!.WaitAsync(Deadline);
            }
            else
            {
                Term result = await operation!.WaitAsync(Deadline);
                Term[] values = Cons.Items(result).ToArray();
                Require(values.Length == expected && values.All(value => value.Equals(Term.I(Value))));
            }
            await retirement.WaitAsync(Deadline);
            Require(PhysicalFreed(native));
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            // Always unblock the actual scheduled continuation before any actor,
            // native-retirement or VM join, including failed preconditions.
            fence.Release();
            if (operation is not null)
            {
                try
                {
                    _ = await operation.WaitAsync(Deadline);
                }
                catch (OperationCanceledException) when (cancel && context?.Cancellation.IsCancellationRequested == true)
                {
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            if (native is not null)
            {
                try
                {
                    await ((Task)Call(native, Close)!).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            if (stopping is not null)
            {
                try
                {
                    await stopping.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            if (actor is not null)
            {
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        Finish(errors);
    }

    internal static async Task LegacyDomainControls()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (Term value in new Term[] { new MapTerm([new KeyValuePair<Term, Term>(Term.I(1), new BitString([1, 2, 3]))]), new TupleTerm([]) })
        {
            await actor.InvokeAsync(async context =>
            {
                Term specification = Term.List(Term.Tuple(Term.A(MatchSpecNames.Wildcard), Nil.Value,
                    Term.List(Term.Tuple(Term.A(MatchSpecNames.Constant), value))));
                Term compiled = await EtsCompiled419Access.Call(context, EtsCompiled419Names.Compile, specification);
                Term result = await EtsCompiled419Access.Call(
                    context,
                    EtsCompiled419Names.Run,
                    Term.List(Term.I(1)),
                    compiled,
                    Nil.Value
                );
                Require(Cons.Items(result).Single().Equals(value));

                return true;
            });
        }
    }

    internal static async Task ForeignPublication()
    {
        await using var first = new ProcessRuntime(1);
        await using var second = new ProcessRuntime(1);
        Install(first);
        Install(second);
        await using var actor = new RuntimeNodeFoundationHostActor(first);
        await using var other = new RuntimeNodeFoundationHostActor(second);
        var published = await actor.Invoke(PublishNative);
        try
        {
            bool refused = await other.Invoke(context =>
            {
                try
                {
                    _ = Call(
                        second,
                        Publish,
                        context,
                        Wrap(published.Native)
                    );

                    return false;
                }
                catch (ArgumentException)
                {
                    return true;
                }
            });
            Require(refused);
        }
        finally
        {
            await ((Task)Call(published.Native, Close)!).WaitAsync(Deadline);
        }
    }

    private static void Finish(List<Exception> errors)
    {
        if (errors.Count == 1)
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException(Failure, errors);
    }
}
