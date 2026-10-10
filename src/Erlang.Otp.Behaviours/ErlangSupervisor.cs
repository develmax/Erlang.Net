// Modified: OTP-29.1.1 supervisor init/check_flags/check_childspec/start MFA boundaries.
namespace Erlang.Otp;

public static class ErlangSupervisor
{
    public static async ValueTask<SupervisorConfiguration> Initialize(ProcessContext context, string module, Term arguments)
    {
        Term value = await context.Runtime.Modules.Call(
            context,
            module,
            BehaviourCallbacks.Init,
            arguments
        );
        if (value is Atom { Name: BehaviourAtoms.Ignore })
            return new([], Ignore: true);
        if (value is not TupleTerm { Items.Count: 2 } ok || ok.Items[0] is not Atom { Name: BehaviourAtoms.Ok } || ok.Items[1] is not TupleTerm { Items.Count: 2 } data)
            throw Exit(Term.Tuple(Term.A(SupervisorAtoms.BadReturn), Term.Tuple(Term.A(module), Term.A(BehaviourCallbacks.Init), value)));
        MapTerm flags;
        if (data.Items[0] is MapTerm map)
            flags = map;
        else if (data.Items[0] is TupleTerm { Items.Count: 3 } tuple)
            flags = new([new(Term.A(SupervisorAtoms.Strategy), tuple.Items[0]), new(Term.A(SupervisorAtoms.Intensity), tuple.Items[1]), new(Term.A(SupervisorAtoms.Period), tuple.Items[2])]);
        else
            throw Flag(SupervisorAtoms.InvalidType, data.Items[0]);
        Term strategyValue = Get(flags, SupervisorAtoms.Strategy, Term.A(SupervisorAtoms.OneForOne));
        RestartStrategy strategy = strategyValue switch
        {
            Atom { Name: SupervisorAtoms.OneForOne } => RestartStrategy.OneForOne,
            Atom { Name: SupervisorAtoms.OneForAll } => RestartStrategy.OneForAll,
            Atom { Name: SupervisorAtoms.RestForOne } => RestartStrategy.RestForOne,
            _ => throw Flag(SupervisorAtoms.InvalidStrategy, strategyValue)
        };
        Term intensity = Get(flags, SupervisorAtoms.Intensity, Term.I(BehaviourDefaults.RestartIntensity));
        Term period = Get(flags, SupervisorAtoms.Period, Term.I(BehaviourDefaults.RestartPeriodSeconds));
        if (intensity is not Integer n || n.Value < 0 || n.Value > int.MaxValue)
            throw Flag(SupervisorAtoms.InvalidIntensity, intensity);
        if (period is not Integer p || p.Value <= 0 || p.Value > int.MaxValue)
            throw Flag(SupervisorAtoms.InvalidPeriod, period);
        Term automatic = Get(flags, SupervisorAtoms.AutoShutdown, Term.A(SupervisorAtoms.Never));
        if (automatic is not Atom { Name: SupervisorAtoms.Never })
            throw Flag(SupervisorAtoms.InvalidAutoShutdown, automatic);
        try
        {
            return new(
                ParseChildren(data.Items[1]),
                strategy,
                (int)n.Value,
                TimeSpan.FromSeconds((int)p.Value)
            );
        }
        catch (ErlangException exception)
        {
            throw Exit(Term.Tuple(Term.A(SupervisorAtoms.StartSpec), exception.Reason));
        }
    }

    public static IReadOnlyList<ChildSpec> ParseChildren(Term value)
    {
        var children = new List<ChildSpec>();
        var ids = new HashSet<Term>();
        Term remaining = value;
        while (remaining is Cons cell)
        {
            var spec = ParseChild(cell.Head);
            if (!ids.Add(spec.Id))
                throw Error(SupervisorAtoms.DuplicateChild, spec.Id);
            children.Add(spec);
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw new ErlangException(Term.Tuple(Term.A(SupervisorAtoms.InvalidChildSpec), value));

        return children;
    }

    private static ChildSpec ParseChild(Term value)
    {
        MapTerm spec;
        if (value is MapTerm map)
            spec = map;
        else if (value is TupleTerm { Items.Count: 6 } tuple)
            spec = new([new(Term.A(SupervisorAtoms.Id), tuple.Items[0]), new(Term.A(SupervisorAtoms.Start), tuple.Items[1]), new(Term.A(SupervisorAtoms.Restart), tuple.Items[2]), new(Term.A(SupervisorAtoms.Shutdown), tuple.Items[3]), new(Term.A(SupervisorAtoms.Type), tuple.Items[4]), new(Term.A(SupervisorAtoms.Modules), tuple.Items[5])]);
        else
            throw Error(SupervisorAtoms.InvalidChildSpec, value);
        if (!spec.TryGet(Term.A(SupervisorAtoms.Id), out var id))
            throw new ErlangException(SupervisorAtoms.MissingId);
        if (!spec.TryGet(Term.A(SupervisorAtoms.Start), out var start))
            throw new ErlangException(SupervisorAtoms.MissingStart);
        if (start is not TupleTerm { Items.Count: 3 } mfa || mfa.Items[0] is not Atom module || mfa.Items[1] is not Atom function)
            throw Error(SupervisorAtoms.InvalidMfa, start!);
        Term[] arguments;
        try
        {
            arguments = Cons.Items(mfa.Items[2]).ToArray();
        }
        catch (ErlangException)
        {
            throw Error(SupervisorAtoms.InvalidMfa, start!);
        }
        Term restartValue = Get(spec, SupervisorAtoms.Restart, Term.A(SupervisorAtoms.Permanent));
        RestartPolicy restart = restartValue switch
        {
            Atom { Name: SupervisorAtoms.Permanent } => RestartPolicy.Permanent,
            Atom { Name: SupervisorAtoms.Transient } => RestartPolicy.Transient,
            Atom { Name: SupervisorAtoms.Temporary } => RestartPolicy.Temporary,
            _ => throw Error(SupervisorAtoms.InvalidRestart, restartValue)
        };
        Term significant = Get(spec, SupervisorAtoms.Significant, Term.A(ErlangBooleanAtoms.False));
        if (significant.Equals(Term.A(ErlangBooleanAtoms.True)))
            throw Error(
                SupervisorAtoms.BadCombination,
                Term.List(
                    Term.Tuple(Term.A(SupervisorAtoms.AutoShutdown), Term.A(SupervisorAtoms.Never)),
                    Term.Tuple(Term.A(SupervisorAtoms.Significant), significant)
                )
            );
        if (!significant.Equals(Term.A(ErlangBooleanAtoms.False)))
            throw Error(SupervisorAtoms.InvalidSignificant, significant);
        Term type = Get(spec, SupervisorAtoms.Type, Term.A(SupervisorAtoms.Worker));
        if (type is not Atom { Name: SupervisorAtoms.Worker or SupervisorAtoms.Supervisor })
            throw Error(SupervisorAtoms.InvalidChildType, type);
        Term shutdown = Get(
            spec,
            SupervisorAtoms.Shutdown,
            type is Atom { Name: SupervisorAtoms.Supervisor } ? Term.A(BehaviourAtoms.Infinity) : Term.I(BehaviourDefaults.ChildShutdownMilliseconds)
        );
        bool infinite = shutdown is Atom { Name: BehaviourAtoms.Infinity };
        bool brutal = shutdown is Atom { Name: SupervisorAtoms.BrutalKill };
        if (!infinite && !brutal && (shutdown is not Integer timeout || timeout.Value < 0 || timeout.Value > int.MaxValue))
            throw Error(SupervisorAtoms.InvalidShutdown, shutdown);
        Term modules = Get(spec, SupervisorAtoms.Modules, Term.List(module));
        if (modules is not Atom { Name: SupervisorAtoms.Dynamic })
        {
            Term[] values;
            try
            {
                values = Cons.Items(modules).ToArray();
            }
            catch (ErlangException)
            {
                throw Error(SupervisorAtoms.InvalidModules, modules);
            }
            foreach (Term item in values)
                if (item is not Atom)
                    throw Error(SupervisorAtoms.InvalidModule, item);
        }

        async ValueTask<ProcessHandle?> Start(ProcessContext context)
        {
            Term result;
            try
            {
                result = await context.Runtime.Modules.Call(
                    context,
                    module.Name,
                    function.Name,
                    arguments
                );
            }
            catch (ErlangException ex) when (ex.ExceptionClass == ErlangExceptionClasses.Throw)
            {
                result = ex.Reason;
            }
            catch (ErlangException ex)
            {
                throw new ErlangException(
                    Term.Tuple(
                        Term.A(ProcessMessageTags.LinkedExit),
                        ex.ExceptionClass == ErlangExceptionClasses.Exit ? ex.Reason : Term.Tuple(ex.Reason, ex.StackTraceTerm)
                    ),
                    ErlangExceptionClasses.Exit
                );
            }
            if (result is Atom { Name: BehaviourAtoms.Ignore })
                return null;
            if (result is TupleTerm { Items.Count: 2 } error && error.Items[0] is Atom { Name: BehaviourAtoms.Error })
                throw new ErlangException(error.Items[1], ErlangExceptionClasses.Exit);
            if (result is TupleTerm success && success.Items.Count is 2 or 3 && success.Items[0] is Atom { Name: BehaviourAtoms.Ok } && success.Items[1] is Pid pid)
                return context.Runtime.GetProcessHandle(pid);

            throw new ErlangException(result, ErlangExceptionClasses.Exit);
        }

        return new(
            id!,
            context => throw new ErlangException(ErlangErrorReasons.UndefinedFunction),
            restart,
            shutdown is Integer millis ? TimeSpan.FromMilliseconds((int)millis.Value) : null,
            Start,
            type,
            modules,
            infinite,
            brutal
        );
    }

    private static Term Get(MapTerm map, string key, Term fallback) => map.TryGet(Term.A(key), out var value) ? value! : fallback;

    private static ErlangException Flag(string reason, Term value) => Exit(Term.Tuple(Term.A(SupervisorAtoms.SupervisorData), Term.Tuple(Term.A(reason), value)));

    private static ErlangException Error(string reason, Term value) => new(Term.Tuple(Term.A(reason), value));

    private static ErlangException Exit(Term reason) => new(reason, ErlangExceptionClasses.Exit);
}
