// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP-29.1.1 specification validation and initialization.
namespace Erlang;

public static partial class SupervisorModule
{
    private static SupervisorChild ParseChild(Term value, Term automatic)
    {
        MapTerm map;
        if (value is MapTerm original)
        {
            map = original;
        }
        else if (value is TupleTerm { Items.Count: 6 } tuple)
        {
            string[] keys = [SupervisorProtocol.Id, SupervisorProtocol.Start, SupervisorProtocol.Restart, SupervisorProtocol.Shutdown, SupervisorProtocol.Type, SupervisorProtocol.Modules];
            map = new MapTerm(keys.Select((key, index) => new KeyValuePair<Term, Term>(A(key), tuple.Items[index])));
        }
        else
        {
            Invalid(SupervisorProtocol.InvalidChildSpec, value);
            throw new InvalidOperationException();
        }
        if (!map.TryGet(A(SupervisorProtocol.Id), out Term? id))
        {
            throw new ErlangException(A(SupervisorProtocol.MissingId), SupervisorProtocol.Throw);
        }
        if (!map.TryGet(A(SupervisorProtocol.Start), out Term? start))
        {
            throw new ErlangException(A(SupervisorProtocol.MissingStart), SupervisorProtocol.Throw);
        }
        if (start is not TupleTerm { Items.Count: 3 } mfa || mfa.Items[0] is not Atom || mfa.Items[1] is not Atom || !IsList(mfa.Items[2]))
        {
            Invalid(SupervisorProtocol.InvalidMfa, start!);
        }
        var function = (TupleTerm)start!;
        Term restart = Get(map, SupervisorProtocol.Restart, A(SupervisorProtocol.Permanent));
        if (!Is(
            restart,
            SupervisorProtocol.Permanent,
            SupervisorProtocol.Transient,
            SupervisorProtocol.Temporary
        ))
        {
            Invalid(SupervisorProtocol.InvalidRestartType, restart);
        }
        Term significant = Get(map, SupervisorProtocol.Significant, A(SupervisorProtocol.False));
        if (Is(significant, SupervisorProtocol.True) && Is(automatic, SupervisorProtocol.Never))
        {
            Invalid(
                SupervisorProtocol.BadCombination,
                Term.List(T(A(SupervisorProtocol.AutoShutdown), automatic), T(A(SupervisorProtocol.Significant), significant))
            );
        }
        if (Is(significant, SupervisorProtocol.True) && Is(restart, SupervisorProtocol.Permanent))
        {
            Invalid(
                SupervisorProtocol.BadCombination,
                Term.List(T(A(SupervisorProtocol.Restart), restart), T(A(SupervisorProtocol.Significant), significant))
            );
        }
        if (!Is(significant, SupervisorProtocol.True, SupervisorProtocol.False))
        {
            Invalid(SupervisorProtocol.InvalidSignificant, significant);
        }
        Term type = Get(map, SupervisorProtocol.Type, A(SupervisorProtocol.Worker));
        if (!Is(type, SupervisorProtocol.Worker, SupervisorProtocol.Supervisor))
        {
            Invalid(SupervisorProtocol.InvalidChildType, type);
        }
        Term shutdown = Get(
            map,
            SupervisorProtocol.Shutdown,
            Is(type, SupervisorProtocol.Worker) ? Term.I(SupervisorDefaults.WorkerShutdownMilliseconds) : A(SupervisorProtocol.Infinity)
        );
        if (!Is(shutdown, SupervisorProtocol.Infinity, SupervisorProtocol.BrutalKill) && shutdown is not Integer { Value.Sign: >= 0 })
        {
            Invalid(SupervisorProtocol.InvalidShutdown, shutdown);
        }
        Term modules = Get(map, SupervisorProtocol.Modules, Term.List(function.Items[0]));
        if (!Is(modules, SupervisorProtocol.Dynamic))
        {
            if (!IsList(modules))
            {
                Invalid(SupervisorProtocol.InvalidModules, modules);
            }
            Term rest = modules;
            while (rest is Cons cell)
            {
                if (cell.Head is not Atom)
                {
                    Invalid(SupervisorProtocol.InvalidModule, cell.Head);
                }
                rest = cell.Tail;
            }
            if (rest is not Nil)
            {
                throw new ErlangException(SupervisorProtocol.FunctionClause);
            }
        }

        return new(
            A(SupervisorProtocol.Undefined),
            id!,
            function,
            restart,
            significant,
            shutdown,
            type,
            modules
        );
    }

    private static List<SupervisorChild> ParseChildren(Term specifications, Term automatic)
    {
        var children = new List<SupervisorChild>();
        var ids = new HashSet<Term>();
        Term rest = specifications;
        while (rest is Cons cell)
        {
            SupervisorChild child = ParseChild(cell.Head, automatic);
            if (!ids.Add(child.Id))
            {
                Invalid(SupervisorProtocol.DuplicateChildName, child.Id);
            }
            children.Add(child);
            rest = cell.Tail;
        }
        if (rest is not Nil)
        {
            throw new ErlangException(SupervisorProtocol.FunctionClause);
        }

        return children;
    }

    private static void SetFlags(SupervisorState state, Term value)
    {
        MapTerm map;
        if (value is MapTerm original)
        {
            map = original;
        }
        else if (value is TupleTerm { Items.Count: 3 } tuple)
        {
            map = new MapTerm(new Dictionary<Term, Term>
            {
                [A(SupervisorProtocol.Strategy)] = tuple.Items[0],
                [A(SupervisorProtocol.Intensity)] = tuple.Items[1],
                [A(SupervisorProtocol.Period)] = tuple.Items[2]
            });
        }
        else
        {
            Invalid(SupervisorProtocol.InvalidType, value);
            throw new InvalidOperationException();
        }
        Term strategy = Get(map, SupervisorProtocol.Strategy, A(SupervisorProtocol.OneForOne));
        Term intensity = Get(map, SupervisorProtocol.Intensity, Term.I(SupervisorDefaults.RestartIntensity));
        Term period = Get(map, SupervisorProtocol.Period, Term.I(SupervisorDefaults.RestartPeriodSeconds));
        Term automatic = Get(map, SupervisorProtocol.AutoShutdown, A(SupervisorProtocol.Never));
        Term hibernate = Get(
            map,
            SupervisorProtocol.HibernateAfter,
            Is(strategy, SupervisorProtocol.SimpleOneForOne) ? A(SupervisorProtocol.Infinity) : Term.I(SupervisorDefaults.HibernateAfterMilliseconds)
        );
        if (!Is(
            strategy,
            SupervisorProtocol.SimpleOneForOne,
            SupervisorProtocol.OneForOne,
            SupervisorProtocol.OneForAll,
            SupervisorProtocol.RestForOne
        ))
        {
            Invalid(SupervisorProtocol.InvalidStrategy, strategy);
        }
        if (intensity is not Integer { Value.Sign: >= 0 })
        {
            Invalid(SupervisorProtocol.InvalidIntensity, intensity);
        }
        if (period is not Integer { Value.Sign: > 0 })
        {
            Invalid(SupervisorProtocol.InvalidPeriod, period);
        }
        if (!Is(
            automatic,
            SupervisorProtocol.Never,
            SupervisorProtocol.AnySignificant,
            SupervisorProtocol.AllSignificant
        ))
        {
            Invalid(SupervisorProtocol.InvalidAutoShutdown, automatic);
        }
        if (Is(strategy, SupervisorProtocol.SimpleOneForOne) && !Is(automatic, SupervisorProtocol.Never))
        {
            Invalid(
                SupervisorProtocol.BadCombination,
                Term.List(T(A(SupervisorProtocol.AutoShutdown), automatic), T(A(SupervisorProtocol.Strategy), strategy))
            );
        }
        if (!Is(hibernate, SupervisorProtocol.Infinity) && hibernate is not Integer { Value.Sign: >= 0 })
        {
            Invalid(SupervisorProtocol.InvalidHibernateAfter, hibernate);
        }
        state.Strategy = strategy;
        state.Intensity = intensity;
        state.Period = period;
        state.AutoShutdown = automatic;
        state.HibernateAfter = hibernate;
    }

    private static async ValueTask<Term> Initialize(ProcessContext context, Term argument)
    {
        var a = Tuple(argument, 3).Items;
        context.TrapExits = true;
        Term result;
        using (SupervisorSource.Scope(
            context,
            SupervisorProtocol.Init,
            1,
            SupervisorSourceLines.InitCallback
        ))
        {
            result = await Call(
                context,
                SupervisorProtocol.Erlang,
                SupervisorProtocol.Apply,
                a[1],
                A(SupervisorProtocol.Init),
                Term.List(a[2])
            );
        }
        if (Is(result, SupervisorProtocol.Ignore))
        {
            return result;
        }
        if (result is not TupleTerm { Items.Count: 2 } ok || !Is(ok.Items[0], SupervisorProtocol.Ok) || ok.Items[1] is not TupleTerm { Items.Count: 2 } data)
        {
            return T(A(SupervisorProtocol.Stop), T(A(SupervisorProtocol.BadReturn), T(a[1], A(SupervisorProtocol.Init), result)));
        }
        Term name = Is(a[0], SupervisorProtocol.Self) ? T(context.Self, a[1]) : a[0];
        var state = new SupervisorState(
            name,
            a[1],
            a[2],
            await Call(context, SupervisorProtocol.Erlang, SupervisorProtocol.MakeRef)
        );
        try
        {
            SetFlags(state, data.Items[0]);
        }
        catch (ErlangException error) when (error.ExceptionClass == SupervisorProtocol.Throw)
        {
            return T(A(SupervisorProtocol.Stop), T(A(SupervisorProtocol.SupervisorData), error.Reason));
        }
        if (state.Simple && data.Items[1] is not Cons { Tail: Nil })
        {
            return T(A(SupervisorProtocol.Stop), T(A(SupervisorProtocol.BadStartSpec), data.Items[1]));
        }
        try
        {
            state.Children.AddRange(ParseChildren(data.Items[1], state.AutoShutdown));
        }
        catch (ErlangException error) when (error.ExceptionClass == SupervisorProtocol.Throw)
        {
            return T(A(SupervisorProtocol.Stop), T(A(SupervisorProtocol.StartSpec), error.Reason));
        }
        if (state.Simple)
        {
            state.DynamicKind = A(state.Children[0].Temporary ? SupervisorProtocol.Mapsets : SupervisorProtocol.Maps);
        }
        else
        {
            Term? failure = await StartChildren(context, state, state.Children.ToArray());
            if (failure is not null)
            {
                await TerminateChildren(context, state, state.Children.ToArray());

                return T(A(SupervisorProtocol.Stop), T(A(SupervisorProtocol.Shutdown), failure));
            }
        }

        return T(A(SupervisorProtocol.Ok), state.ToTerm(), Action(state));
    }

    private static async ValueTask<Term> CodeChange(ProcessContext context, SupervisorState state)
    {
        Term result;
        using (SupervisorSource.Scope(
            context,
            SupervisorProtocol.CodeChange,
            3,
            SupervisorSourceLines.UpgradeCallback
        ))
        {
            result = await Call(
                context,
                SupervisorProtocol.Erlang,
                SupervisorProtocol.Apply,
                state.Module,
                A(SupervisorProtocol.Init),
                Term.List(state.Args)
            );
        }
        if (Is(result, SupervisorProtocol.Ignore))
        {
            return T(A(SupervisorProtocol.Ok), state.ToTerm());
        }
        if (result is not TupleTerm { Items.Count: 2 } ok || !Is(ok.Items[0], SupervisorProtocol.Ok) || ok.Items[1] is not TupleTerm { Items.Count: 2 } data)
        {
            return result;
        }
        try
        {
            SetFlags(state, data.Items[0]);
        }
        catch (ErlangException error) when (error.ExceptionClass == SupervisorProtocol.Throw)
        {
            Term reason = error.Reason;
            if (reason is TupleTerm pair && Is(pair.Items[0], SupervisorProtocol.InvalidType))
            {
                reason = T(A(SupervisorProtocol.BadFlags), data.Items[0]);
            }

            return Error(reason);
        }
        List<SupervisorChild> children;
        try
        {
            children = ParseChildren(data.Items[1], state.AutoShutdown);
        }
        catch (ErlangException error) when (error.ExceptionClass == SupervisorProtocol.Throw)
        {
            return Error(error.Reason);
        }
        if (state.Simple)
        {
            if (children.Count != 1)
            {
                return Error(T(
                    A(SupervisorProtocol.Ok),
                    T(Cons.From(children.Select(c => c.Id)), new MapTerm(children.Select(c => new KeyValuePair<Term, Term>(c.Id, c.ToTerm()))))
                ));
            }
            state.Children.Clear();
            state.Children.AddRange(children);
        }
        else
        {
            var old = state.Children.ToDictionary(c => c.Id);
            var keep = state.Children.Where(c => children.All(n => !n.Id.Equals(c.Id))).ToArray();
            state.Children.Clear();
            state.Children.AddRange(keep);
            state.Children.AddRange(children.AsEnumerable().Reverse().Select(c => old.TryGetValue(c.Id, out var previous) ? c with { Pid = previous.Pid } : c));
        }

        return T(A(SupervisorProtocol.Ok), state.ToTerm());
    }
}
