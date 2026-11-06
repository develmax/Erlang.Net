// Derived from lib/kernel/src/application.erl, Erlang/OTP 29.1.1, Apache-2.0.
// Copyright Ericsson AB 1996-2026. Original notices remain in Otp/Original.
namespace Erlang;

/// <summary>Selectable application library using the original OTP controller protocols.</summary>
public static partial class ApplicationModule
{
    public const string Name = "application";

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (ApplicationApi.EnsureAllStarted, 1), (ApplicationApi.EnsureAllStarted, 2), (ApplicationApi.EnsureAllStarted, 3),
        (ApplicationApi.Start, 1), (ApplicationApi.Start, 2),
        (ApplicationApi.StartBoot, 1), (ApplicationApi.StartBoot, 2), (ApplicationApi.Stop, 1),
        (ApplicationApi.Load, 1), (ApplicationApi.Load, 2), (ApplicationApi.Unload, 1), (ApplicationApi.Takeover, 2),
        (ApplicationApi.WhichApplications, 0), (ApplicationApi.WhichApplications, 1),
        (ApplicationApi.LoadedApplications, 0), (ApplicationApi.Permit, 2),
        (ApplicationApi.EnsureStarted, 1), (ApplicationApi.EnsureStarted, 2),
        (ApplicationApi.SetEnv, 1), (ApplicationApi.SetEnv, 2), (ApplicationApi.SetEnv, 3), (ApplicationApi.SetEnv, 4),
        (ApplicationApi.UnsetEnv, 2), (ApplicationApi.UnsetEnv, 3),
        (ApplicationApi.GetEnv, 1), (ApplicationApi.GetEnv, 2), (ApplicationApi.GetEnv, 3),
        (ApplicationApi.GetAllEnv, 0), (ApplicationApi.GetAllEnv, 1),
        (ApplicationApi.GetKey, 1), (ApplicationApi.GetKey, 2),
        (ApplicationApi.GetAllKey, 0), (ApplicationApi.GetAllKey, 1),
        (ApplicationApi.GetApplication, 0), (ApplicationApi.GetApplication, 1),
        (ApplicationApi.GetSupervisor, 1), (ApplicationApi.Info, 0), (ApplicationApi.StartType, 0),
        (ApplicationApi.BehaviourInfo, 1)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallPreloaded(preloaded => preloaded.InstallSourceWrappers(target =>
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
        }));

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> args)
    {
        if (!Exports.Contains((function, args.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        using var tail = context.EnterSourceTailCall(Name, function);
        switch (function)
        {
            case ApplicationApi.Load:
                return await Load(context, args[0], args.Count == 2 ? args[1] : Nil.Value);
            case ApplicationApi.Start:
            case ApplicationApi.EnsureStarted:
                {
                    using var ensureFrame = function == ApplicationApi.EnsureStarted
                        ? ApplicationSource.Scope(
                            context,
                            ApplicationApi.EnsureStarted,
                            2,
                            ApplicationSource.EnsureStarted
                        )
                        : null;
                    Term result = await Start(context, args[0], args.Count == 2 ? args[1] : A(ApplicationProtocol.Temporary));

                    return function == ApplicationApi.EnsureStarted && IsTagged(result, ApplicationProtocol.Error, out Term reason)
                        && IsTagged(reason, ApplicationProtocol.AlreadyStarted, out Term app) && app.Equals(args[0])
                        ? A(ApplicationProtocol.Ok) : result;
                }
            case ApplicationApi.EnsureAllStarted:
                return await EnsureAllStarted(
                    context,
                    args[0],
                    args.Count > 1 ? args[1] : A(ApplicationProtocol.Temporary),
                    args.Count > 2 ? args[2] : A(ApplicationProtocol.Serial)
                );
            case ApplicationApi.StartBoot:
                return await Controller(
                    context,
                    ApplicationProtocol.StartBootApplication,
                    args[0],
                    args.Count == 2 ? args[1] : A(ApplicationProtocol.Temporary)
                );
            case ApplicationApi.Unload:
                return await Controller(context, ApplicationProtocol.UnloadApplication, args[0]);
            case ApplicationApi.Stop:
                return await Controller(context, ApplicationProtocol.StopApplication, args[0]);
            case ApplicationApi.Takeover:
                return await Call(
                    context,
                    ApplicationProtocol.Distribution,
                    ApplicationProtocol.TakeoverApplication,
                    args.ToArray()
                );
            case ApplicationApi.Permit:
                return await Permit(context, args[0], args[1]);
            case ApplicationApi.WhichApplications:
                if (args.Count == 1 && !Timeout(args[0]))
                {
                    throw ApplicationSource.Clause(
                        context,
                        function,
                        args,
                        ApplicationSource.WhichApplications
                    );
                }

                return await Controller(context, function, args.ToArray());
            case ApplicationApi.LoadedApplications:
            case ApplicationApi.Info:
                return await Controller(context, function);
            case ApplicationApi.SetEnv:
                return await SetEnv(context, args);
            case ApplicationApi.UnsetEnv:
                {
                    if (args.Count == 2)
                    {
                        return await Controller(context, function, args.ToArray());
                    }
                    Term options = Options(
                        context,
                        function,
                        args,
                        ApplicationSource.UnsetEnv
                    );

                    return await Controller(
                        context,
                        function,
                        args[0],
                        args[1],
                        options
                    );
                }
            case ApplicationApi.GetEnv:
            case ApplicationApi.GetKey:
                return args.Count == 1
                    ? await Controller(
                        context,
                        function == ApplicationApi.GetEnv ? ApplicationProtocol.GetPidEnv : ApplicationProtocol.GetPidKey,
                        await GroupLeader(context),
                        args[0]
                    )
                    : await Controller(context, function, args.ToArray());
            case ApplicationApi.GetAllEnv:
            case ApplicationApi.GetAllKey:
                return args.Count == 0
                    ? await Controller(
                        context,
                        function == ApplicationApi.GetAllEnv ? ApplicationProtocol.GetPidAllEnv : ApplicationProtocol.GetPidAllKey,
                        await GroupLeader(context)
                    )
                    : await Controller(context, function, args[0]);
            case ApplicationApi.GetApplication:
                return await GetApplication(context, args);
            case ApplicationApi.GetSupervisor:
                return await GetSupervisor(context, args[0]);
            case ApplicationApi.StartType:
                return await Controller(context, function, await GroupLeader(context));
            case ApplicationApi.BehaviourInfo:
                return BehaviourInfo(context, args[0]);
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    private static Term A(string name) => Term.A(name);

    private static ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] args
    )
        => context.Runtime.Modules.Call(
            context,
            module,
            function,
            args
        );

    private static ValueTask<Term> Controller(ProcessContext context, string function, params Term[] args)
        => Call(
            context,
            ApplicationProtocol.Controller,
            function,
            args
        );

    private static ValueTask<Term> GroupLeader(ProcessContext context)
        => Call(context, ApplicationProtocol.Erlang, ApplicationProtocol.GroupLeader);

    private static bool Is(Term value, string atom) => value.Equals(A(atom));

    private static bool IsTagged(Term value, string tag, out Term payload)
    {
        if (value is TupleTerm { Items.Count: 2 } tuple && Is(tuple.Items[0], tag))
        {
            payload = tuple.Items[1];

            return true;
        }
        payload = Nil.Value;

        return false;
    }

    private static bool IsList(Term value) => value is Cons or Nil;

    private static bool Timeout(Term value)
        => Is(value, ApplicationProtocol.Infinity) || value is Integer number && number.Value >= 0;

    private static Term Options(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> args,
        int line
    )
    {
        Term value = args[^1];
        if (Timeout(value))
        {
            return Term.List(Term.Tuple(A(ApplicationProtocol.Timeout), value));
        }
        if (IsList(value))
        {
            return value;
        }

        throw ApplicationSource.Clause(
            context,
            function,
            args,
            line
        );
    }

    private static Term AppName(ProcessContext context, Term app)
    {
        if (app is Atom)
        {
            return app;
        }
        if (app is TupleTerm { Items.Count: 3 } tuple && Is(tuple.Items[0], Name) && tuple.Items[1] is Atom)
        {
            return tuple.Items[1];
        }

        throw ApplicationSource.Clause(
            context,
            ApplicationProtocol.GetApplName,
            [app],
            ApplicationSource.GetApplName
        );
    }

    private static async ValueTask<Term> Load(ProcessContext context, Term app, Term nodes)
    {
        Term result;
        using (ApplicationSource.Scope(
            context,
            ApplicationProtocol.LoadInternal,
            2,
            ApplicationSource.LoadController
        ))
        {
            result = await Controller(context, ApplicationProtocol.LoadApplication, app);
        }
        if (!Is(result, ApplicationProtocol.Ok) || nodes is Nil)
        {
            return result;
        }
        Term name;
        using (ApplicationSource.Scope(
            context,
            ApplicationProtocol.LoadInternal,
            2,
            ApplicationSource.LoadName
        ))
        {
            name = AppName(context, app);
        }
        Term distributed;
        using (ApplicationSource.Scope(
            context,
            ApplicationProtocol.LoadInternal,
            2,
            ApplicationSource.LoadDistribution
        ))
        {
            distributed = await Call(
                context,
                ApplicationProtocol.Distribution,
                ApplicationProtocol.LoadApplication,
                name,
                nodes
            );
        }
        if (Is(distributed, ApplicationProtocol.Ok))
        {
            return distributed;
        }
        if (IsTagged(distributed, ApplicationProtocol.Error, out _))
        {
            using var frame = ApplicationSource.Scope(
                context,
                ApplicationProtocol.LoadInternal,
                2,
                ApplicationSource.LoadRollback
            );
            await Controller(context, ApplicationProtocol.UnloadApplication, name);

            return distributed;
        }

        throw ApplicationSource.Case(
            context,
            ApplicationProtocol.LoadInternal,
            2,
            distributed,
            ApplicationSource.LoadDistribution
        );
    }

    private static async ValueTask<Term> EnsureLoaded(ProcessContext context, Term app)
    {
        using var frame = ApplicationSource.Scope(
            context,
            ApplicationProtocol.EnsureLoaded,
            1,
            ApplicationSource.EnsureLoaded
        );
        Term result = await Load(context, app, Nil.Value);
        if (Is(result, ApplicationProtocol.Ok))
        {
            return Term.Tuple(A(ApplicationProtocol.Ok), AppName(context, app));
        }
        if (IsTagged(result, ApplicationProtocol.Error, out Term reason) && IsTagged(reason, ApplicationProtocol.AlreadyLoaded, out Term name))
        {
            return Term.Tuple(A(ApplicationProtocol.Ok), name);
        }

        return result;
    }

    private static async ValueTask<Term> Start(ProcessContext context, Term app, Term type)
    {
        Term result;
        using (ApplicationSource.Scope(
            context,
            ApplicationApi.Start,
            2,
            ApplicationSource.Start
        ))
        {
            result = await EnsureLoaded(context, app);
        }

        return IsTagged(result, ApplicationProtocol.Ok, out Term name)
            ? await Controller(
                context,
                ApplicationProtocol.StartApplication,
                name,
                type
            ) : result;
    }

    private static async ValueTask<Term> Permit(ProcessContext context, Term app, Term permission)
    {
        if (!Is(permission, ApplicationProtocol.True) && !Is(permission, ApplicationProtocol.False))
        {
            throw ApplicationSource.Error(
                context,
                ApplicationApi.Permit,
                Term.I(2),
                Term.Tuple(A(ErlangErrorReasons.BadArgument), Term.Tuple(A(Name), A(ApplicationApi.Permit), Term.List(app, permission))),
                ApplicationSource.PermitInvalid,
                ErlangExceptionClasses.Exit
            );
        }
        Term result;
        using (ApplicationSource.Scope(
            context,
            ApplicationApi.Permit,
            2,
            ApplicationSource.PermitController
        ))
        {
            result = await Controller(
                context,
                ApplicationProtocol.PermitApplication,
                app,
                permission
            );
        }
        if (Is(result, ApplicationProtocol.DistributedApplication))
        {
            return await Call(
                context,
                ApplicationProtocol.Distribution,
                ApplicationProtocol.PermitApplication,
                app,
                permission
            );
        }
        if (IsTagged(result, ApplicationProtocol.DistributedApplication, out Term state) && Is(state, ApplicationProtocol.OnlyLoaded))
        {
            return await Call(
                context,
                ApplicationProtocol.Distribution,
                ApplicationProtocol.PermitOnlyLoadedApplication,
                app,
                permission
            );
        }

        return result;
    }

    private static async ValueTask<Term> SetEnv(ProcessContext context, IReadOnlyList<Term> args)
    {
        if (args.Count <= 2)
        {
            if (!IsList(args[0]) || args.Count == 2 && !IsList(args[1]))
            {
                throw ApplicationSource.Clause(
                    context,
                    ApplicationApi.SetEnv,
                    args,
                    args.Count == 1 ? ApplicationSource.SetEnvOne : ApplicationSource.SetEnvTwo
                );
            }
            Term options = args.Count == 2 ? args[1] : Nil.Value;
            using var frame = ApplicationSource.Scope(
                context,
                ApplicationApi.SetEnv,
                2,
                ApplicationSource.SetEnvController
            );
            Term result = await Controller(
                context,
                ApplicationApi.SetEnv,
                args[0],
                options
            );
            if (Is(result, ApplicationProtocol.Ok))
            {
                return result;
            }
            if (IsTagged(result, ApplicationProtocol.Error, out Term message))
            {
                throw ApplicationSource.Error(
                    context,
                    ApplicationApi.SetEnv,
                    Term.List(args[0], options),
                    Term.Tuple(A(ErlangErrorReasons.BadArgument), message),
                    ApplicationSource.SetEnvInvalid
                );
            }

            throw ApplicationSource.Case(
                context,
                ApplicationApi.SetEnv,
                2,
                result,
                ApplicationSource.SetEnvController
            );
        }
        if (args.Count == 3)
        {
            return await Controller(context, ApplicationApi.SetEnv, args.ToArray());
        }
        Term opts = Options(
            context,
            ApplicationApi.SetEnv,
            args,
            ApplicationSource.SetEnvFour
        );

        return await Controller(
            context,
            ApplicationApi.SetEnv,
            args[0],
            args[1],
            args[2],
            opts
        );
    }

    private static async ValueTask<Term> GetApplication(ProcessContext context, IReadOnlyList<Term> args)
    {
        if (args.Count == 0)
        {
            return await Controller(context, ApplicationApi.GetApplication, await GroupLeader(context));
        }
        if (args[0] is Atom)
        {
            return await Controller(context, ApplicationProtocol.GetApplicationModule, args[0]);
        }
        if (args[0] is not Pid)
        {
            throw ApplicationSource.Clause(
                context,
                ApplicationApi.GetApplication,
                args,
                ApplicationSource.GetApplication
            );
        }
        Term info;
        using (ApplicationSource.Scope(
            context,
            ApplicationApi.GetApplication,
            1,
            ApplicationSource.GetApplicationInfo
        ))
        {
            info = await Call(
                context,
                ApplicationProtocol.Erlang,
                ApplicationProtocol.ProcessInfo,
                args[0],
                A(ApplicationProtocol.GroupLeader)
            );
        }
        if (Is(info, ApplicationProtocol.Undefined))
        {
            return info;
        }
        if (IsTagged(info, ApplicationProtocol.GroupLeader, out Term leader))
        {
            return await Controller(context, ApplicationApi.GetApplication, leader);
        }

        throw ApplicationSource.Case(
            context,
            ApplicationApi.GetApplication,
            1,
            info,
            ApplicationSource.GetApplicationInfo
        );
    }

    private static async ValueTask<Term> GetSupervisor(ProcessContext context, Term app)
    {
        if (app is not Atom)
        {
            throw ApplicationSource.Clause(
                context,
                ApplicationApi.GetSupervisor,
                [app],
                ApplicationSource.GetSupervisor
            );
        }
        Term master;
        using (ApplicationSource.Scope(
            context,
            ApplicationApi.GetSupervisor,
            1,
            ApplicationSource.GetMaster
        ))
        {
            master = await Controller(context, ApplicationProtocol.GetMaster, app);
        }
        if (Is(master, ApplicationProtocol.Undefined))
        {
            return master;
        }
        using var frame = ApplicationSource.Scope(
            context,
            ApplicationApi.GetSupervisor,
            1,
            ApplicationSource.GetChild
        );
        Term child = await Call(
            context,
            ApplicationProtocol.Master,
            ApplicationProtocol.GetChild,
            master
        );
        if (child is TupleTerm { Items.Count: 2 } tuple)
        {
            return Term.Tuple(A(ApplicationProtocol.Ok), tuple.Items[0]);
        }
        if (Is(child, ApplicationProtocol.Error))
        {
            return A(ApplicationProtocol.Undefined);
        }

        throw ApplicationSource.Case(
            context,
            ApplicationApi.GetSupervisor,
            1,
            child,
            ApplicationSource.GetChild
        );
    }

    private static Term BehaviourInfo(ProcessContext context, Term kind)
    {
        if (Is(kind, ApplicationProtocol.Callbacks))
        {
            return Term.List(
                Term.Tuple(A(ApplicationApi.Start), Term.I(2)),
                Term.Tuple(A(ApplicationApi.Stop), Term.I(1)),
                Term.Tuple(A(ApplicationProtocol.ConfigChange), Term.I(3)),
                Term.Tuple(A(ApplicationProtocol.PrepStop), Term.I(1)),
                Term.Tuple(A(ApplicationProtocol.StartPhase), Term.I(3))
            );
        }
        if (Is(kind, ApplicationProtocol.OptionalCallbacks))
        {
            return Term.List(
                Term.Tuple(A(ApplicationProtocol.ConfigChange), Term.I(3)),
                Term.Tuple(A(ApplicationProtocol.PrepStop), Term.I(1)),
                Term.Tuple(A(ApplicationProtocol.StartPhase), Term.I(3))
            );
        }

        throw ApplicationSource.Clause(
            context,
            ApplicationProtocol.InlinedBehaviourInfo,
            [kind],
            0
        );
    }
}
