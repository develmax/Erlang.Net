// Derived from lib/kernel/src/application.erl, Erlang/OTP 29.1.1, Apache-2.0.
// Copyright Ericsson AB 1996-2026. Original notices remain in Otp/Original.
namespace Erlang;

public static partial class ApplicationModule
{
    private sealed record Traversal(
        MapTerm Dag,
        Term Pending,
        Term Started,
        Term? Failure = null
    );

    private sealed record Response(
        Term Requests,
        Term Started,
        Term? App = null,
        Term? Failure = null
    );

    private static async ValueTask<Term> EnsureAllStarted(
        ProcessContext context,
        Term apps,
        Term type,
        Term mode
    )
    {
        if (apps is Atom)
        {
            apps = Term.List(apps);
        }
        if (!IsList(apps))
        {
            throw ApplicationSource.Clause(
                context,
                ApplicationApi.EnsureAllStarted,
                [apps, type, mode],
                ApplicationSource.EnsureAllStarted
            );
        }
        using var frame = ApplicationSource.Scope(
            context,
            ApplicationApi.EnsureAllStarted,
            3,
            ApplicationSource.EnsureAllTraversal
        );
        var options = new MapTerm(new Dictionary<Term, Term>
        {
            [A(ApplicationProtocol.Type)] = type,
            [A(ApplicationProtocol.Mode)] = mode
        });
        Traversal result = await Enqueue(
            context,
            apps,
            Nil.Value,
            new(new MapTerm([]), Nil.Value, Nil.Value),
            options
        );
        if (result.Failure is not null)
        {
            await Rollback(context, result.Started);

            return Term.Tuple(A(ApplicationProtocol.Error), result.Failure);
        }
        if (Is(mode, ApplicationProtocol.Serial))
        {
            if (result.Dag.Entries.Count != 0)
            {
                throw ApplicationSource.Error(
                    context,
                    ApplicationApi.EnsureAllStarted,
                    Term.I(3),
                    Term.Tuple(A(ErlangErrorReasons.BadMatch), Term.I(result.Dag.Entries.Count)),
                    ApplicationSource.EnsureAllEmptyDag
                );
            }

            return Term.Tuple(A(ApplicationProtocol.Ok), Cons.From(Cons.Items(result.Started).Reverse()));
        }
        if (Is(mode, ApplicationProtocol.Concurrent))
        {
            Term pending = await Call(
                context,
                ApplicationProtocol.Maps,
                ApplicationProtocol.ToList,
                result.Dag
            );
            Term requests = await Call(context, ApplicationProtocol.GenServer, ApplicationProtocol.ReqidsNew);

            return await ConcurrentStart(
                context,
                pending,
                requests,
                Nil.Value,
                result.Started,
                type
            );
        }

        throw ApplicationSource.Case(
            context,
            ApplicationApi.EnsureAllStarted,
            3,
            TraversalTerm(result),
            ApplicationSource.EnsureAllTraversal
        );
    }

    private static Term TraversalTerm(Traversal value)
        => value.Failure is { } failure
            ? Term.Tuple(A(ApplicationProtocol.Error), failure, value.Started)
            : Term.Tuple(
                A(ApplicationProtocol.Ok),
                value.Dag,
                value.Pending,
                value.Started
            );

    private static async ValueTask<Traversal> Enqueue(
        ProcessContext context,
        Term apps,
        Term optional,
        Traversal state,
        MapTerm options
    )
    {
        while (apps is Cons cell)
        {
            Term app = cell.Head;
            if (state.Dag.Entries.Any(entry => entry.Key.Equals(app)))
            {
                state = state with { Pending = new Cons(app, state.Pending) };
                apps = cell.Tail;
                continue;
            }
            if (app is not Atom)
            {
                throw ApplicationSource.Clause(
                    context,
                    ApplicationProtocol.EnqueueOrStart,
                    [apps, optional, state.Dag, state.Pending, state.Started, options],
                    ApplicationSource.EnqueueClause
                );
            }
            Term running;
            using (ApplicationSource.Scope(
                context,
                ApplicationProtocol.EnqueueOrStart,
                6,
                ApplicationSource.EnqueueRunning
            ))
            {
                running = await Controller(context, ApplicationProtocol.IsRunning, app);
            }
            if (Is(running, ApplicationProtocol.True))
            {
                apps = cell.Tail;
                continue;
            }
            if (!Is(running, ApplicationProtocol.False))
            {
                throw ApplicationSource.Case(
                    context,
                    ApplicationProtocol.EnqueueOrStart,
                    6,
                    running,
                    ApplicationSource.EnqueueRunning
                );
            }
            Term loaded;
            using (ApplicationSource.Scope(
                context,
                ApplicationProtocol.EnqueueOrStart,
                6,
                ApplicationSource.EnqueueLoad
            ))
            {
                loaded = await EnsureLoaded(context, app);
            }
            if (IsTagged(loaded, ApplicationProtocol.Ok, out Term name))
            {
                using var frame = ApplicationSource.Scope(
                    context,
                    ApplicationProtocol.EnqueueOrStart,
                    6,
                    ApplicationSource.EnqueueChild
                );
                state = await EnqueueApp(
                    context,
                    name,
                    app,
                    state,
                    options
                );
                if (state.Failure is not null)
                {
                    return state;
                }
            }
            else if (IsTagged(loaded, ApplicationProtocol.Error, out Term reason))
            {
                bool missing = reason is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(Term.String(ApplicationProtocol.MissingFile));
                if (!missing || !Cons.Items(optional).Contains(app))
                {
                    return state with { Failure = Term.Tuple(app, reason) };
                }
            }
            else
            {
                throw ApplicationSource.Case(
                    context,
                    ApplicationProtocol.EnqueueOrStart,
                    6,
                    loaded,
                    ApplicationSource.EnqueueLoad
                );
            }
            apps = cell.Tail;
        }
        if (apps is not Nil)
        {
            throw ApplicationSource.Clause(
                context,
                ApplicationProtocol.EnqueueOrStart,
                [apps, optional, state.Dag, state.Pending, state.Started, options],
                ApplicationSource.EnqueueClause
            );
        }

        return state;
    }

    private static async ValueTask<Term> RequiredKey(
        ProcessContext context,
        Term name,
        string key,
        int line
    )
    {
        using var frame = ApplicationSource.Scope(
            context,
            ApplicationProtocol.EnqueueOrStartApp,
            6,
            line
        );
        Term value = await Controller(
            context,
            ApplicationApi.GetKey,
            name,
            A(key)
        );
        if (IsTagged(value, ApplicationProtocol.Ok, out Term payload))
        {
            return payload;
        }

        throw ApplicationSource.Error(
            context,
            ApplicationProtocol.EnqueueOrStartApp,
            Term.I(6),
            Term.Tuple(A(ErlangErrorReasons.BadMatch), value),
            line
        );
    }

    private static async ValueTask<Traversal> EnqueueApp(
        ProcessContext context,
        Term name,
        Term app,
        Traversal state,
        MapTerm options
    )
    {
        Term type = options.Entries.Single(entry => Is(entry.Key, ApplicationProtocol.Type)).Value;
        Term mode = options.Entries.Single(entry => Is(entry.Key, ApplicationProtocol.Mode)).Value;
        Term children = await RequiredKey(
            context,
            name,
            ApplicationProtocol.Applications,
            ApplicationSource.ChildrenKey
        );
        Term optional = await RequiredKey(
            context,
            name,
            ApplicationProtocol.OptionalApplications,
            ApplicationSource.OptionalKey
        );
        Term mod = await RequiredKey(
            context,
            name,
            ApplicationProtocol.Mod,
            ApplicationSource.ModKey
        );
        Traversal childState;
        using (ApplicationSource.Scope(
            context,
            ApplicationProtocol.EnqueueOrStartApp,
            6,
            ApplicationSource.ChildrenTraversal
        ))
        {
            childState = await Enqueue(
                context,
                children,
                optional,
                state with { Pending = Nil.Value },
                options
            );
        }
        if (childState.Failure is not null)
        {
            return childState;
        }
        if (childState.Pending is Nil && (Is(mode, ApplicationProtocol.Serial) || mod is Nil))
        {
            using var frame = ApplicationSource.Scope(
                context,
                ApplicationProtocol.EnqueueOrStartApp,
                6,
                ApplicationSource.ChildStart
            );
            Term start = await Controller(
                context,
                ApplicationProtocol.StartApplication,
                app,
                type
            );
            if (Is(start, ApplicationProtocol.Ok))
            {
                return childState with { Pending = state.Pending, Started = new Cons(app, childState.Started) };
            }
            if (IsTagged(start, ApplicationProtocol.Error, out Term reason))
            {
                return IsTagged(reason, ApplicationProtocol.AlreadyStarted, out Term already) && already.Equals(app)
                    ? childState with { Pending = state.Pending }
                    : childState with { Failure = Term.Tuple(app, reason) };
            }

            throw ApplicationSource.Case(
                context,
                ApplicationProtocol.EnqueueOrStartApp,
                6,
                start,
                ApplicationSource.ChildStart
            );
        }
        var dag = childState.Dag.Entries.ToDictionary(entry => entry.Key, entry => entry.Value);
        dag[app] = childState.Pending;

        return childState with { Dag = new MapTerm(dag), Pending = new Cons(app, state.Pending) };
    }

    private static async ValueTask<Term> ConcurrentStart(
        ProcessContext context,
        Term pending,
        Term requests,
        Term done,
        Term started,
        Term type
    )
    {
        while (pending is not Nil)
        {
            var accumulator = new List<Term>();
            foreach (TupleTerm entry in Cons.Items(pending))
            {
                Term children = await Call(
                    context,
                    ApplicationProtocol.Erlang,
                    ApplicationProtocol.Subtract,
                    entry.Items[1],
                    done
                );
                if (children is Nil)
                {
                    Term request = await Controller(
                        context,
                        ApplicationProtocol.StartApplicationRequest,
                        entry.Items[0],
                        type
                    );
                    requests = await Call(
                        context,
                        ApplicationProtocol.GenServer,
                        ApplicationProtocol.ReqidsAdd,
                        request,
                        entry.Items[0],
                        requests
                    );
                }
                else
                {
                    accumulator.Insert(0, Term.Tuple(entry.Items[0], children));
                }
            }
            Response response = await WaitOne(context, requests, started);
            if (response.Failure is not null)
            {
                return await WaitAll(
                    context,
                    response.Requests,
                    started,
                    response.Failure
                );
            }
            pending = Cons.From(accumulator);
            requests = response.Requests;
            done = Term.List(response.App!);
            started = response.Started;
        }

        return await WaitAll(
            context,
            requests,
            started,
            null
        );
    }

    private static async ValueTask<Response> WaitOne(ProcessContext context, Term requests, Term started)
    {
        using var frame = ApplicationSource.Scope(
            context,
            ApplicationProtocol.WaitOneEnqueued,
            2,
            ApplicationSource.WaitResponse
        );
        Term result = await Call(
            context,
            ApplicationProtocol.GenServer,
            ApplicationProtocol.WaitResponse,
            requests,
            A(ApplicationProtocol.Infinity),
            A(ApplicationProtocol.True)
        );
        if (result is TupleTerm { Items.Count: 3 } tuple)
        {
            Term app = tuple.Items[1];
            Term remaining = tuple.Items[2];
            if (IsTagged(tuple.Items[0], ApplicationProtocol.Reply, out Term reply))
            {
                if (Is(reply, ApplicationProtocol.Ok))
                {
                    return new(remaining, new Cons(app, started), app);
                }
                if (IsTagged(reply, ApplicationProtocol.Error, out Term reason))
                {
                    return IsTagged(reason, ApplicationProtocol.AlreadyStarted, out Term already) && already.Equals(app)
                        ? new(remaining, started, app)
                        : new(remaining, started, Failure: Term.Tuple(app, reason));
                }
            }
            if (IsTagged(tuple.Items[0], ApplicationProtocol.Error, out Term failure) && failure is TupleTerm { Items.Count: 2 } error)
            {
                throw ApplicationSource.Error(
                    context,
                    ApplicationProtocol.WaitOneEnqueued,
                    Term.I(2),
                    error.Items[0],
                    ApplicationSource.WaitExit,
                    ErlangExceptionClasses.Exit
                );
            }
        }
        if (Is(result, ApplicationProtocol.NoRequest))
        {
            throw ApplicationSource.Error(
                context,
                ApplicationProtocol.WaitOneEnqueued,
                Term.I(2),
                A(ApplicationProtocol.Deadlock),
                ApplicationSource.WaitDeadlock,
                ErlangExceptionClasses.Exit
            );
        }

        throw ApplicationSource.Case(
            context,
            ApplicationProtocol.WaitOneEnqueued,
            2,
            result,
            ApplicationSource.WaitResponse
        );
    }

    private static async ValueTask<Term> WaitAll(
        ProcessContext context,
        Term requests,
        Term started,
        Term? failure
    )
    {
        while (true)
        {
            Term size = await Call(
                context,
                ApplicationProtocol.GenServer,
                ApplicationProtocol.ReqidsSize,
                requests
            );
            if (size.Equals(Term.I(0)))
            {
                if (failure is null)
                {
                    return Term.Tuple(A(ApplicationProtocol.Ok), Cons.From(Cons.Items(started).Reverse()));
                }
                await Rollback(context, started);

                return Term.Tuple(A(ApplicationProtocol.Error), failure);
            }
            Response response = await WaitOne(context, requests, started);
            requests = response.Requests;
            if (response.Failure is null)
            {
                started = response.Started;
            }
            else
            {
                failure = response.Failure;
            }
        }
    }

    private static async ValueTask Rollback(ProcessContext context, Term started)
    {
        foreach (Term app in Cons.Items(started))
        {
            await Controller(context, ApplicationProtocol.StopApplication, app);
        }
    }
}
