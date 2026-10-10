using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Erlang.Otp;

public static class OtpModules
{
    private static readonly ConditionalWeakTable<ModuleRegistry, object> Installed = new();

    public static void Register(ModuleRegistry registry)
    {
        lock (Installed)
        {
            if (Installed.TryGetValue(registry, out _))
                return;

            void Add(
                string module,
                string function,
                int arity,
                ErlangFunction body
            ) => registry.Register(
                module,
                function,
                arity,
                body
            );
            foreach (bool link in new[] { false, true })
            {
                string function = link ? OtpNames.StartLink : OtpNames.Start;
                Add(
                    OtpNames.GenServer,
                    function,
                    3,
                    (c, a) => StartServer(
                        c,
                        a,
                        link,
                        false
                    )
                );
                Add(
                    OtpNames.GenServer,
                    function,
                    4,
                    (c, a) => StartServer(
                        c,
                        a,
                        link,
                        true
                    )
                );
            }
            Add(
                OtpNames.GenServer,
                OtpNames.Call,
                2,
                (c, a) => Call(c, a)
            );
            Add(
                OtpNames.GenServer,
                OtpNames.Call,
                3,
                (c, a) => Call(c, a)
            );
            Add(
                OtpNames.GenServer,
                OtpNames.Cast,
                2,
                (c, a) =>
            {
                Pid? pid = Resolve(c, a[0], false);
                if (pid is not null)
                    GenServer.Cast(c, pid, a[1]);

                return ValueTask.FromResult<Term>(Term.A(BehaviourAtoms.Ok));
            }
            );
            Add(
                OtpNames.GenServer,
                OtpNames.Reply,
                2,
                (c, a) =>
            {
                if (a[0] is not TupleTerm { Items.Count: 2 } from || from.Items[0] is not Pid pid || from.Items[1] is not ReferenceTerm reference)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                GenServer.Reply(c, new(pid, reference), a[1]);

                return ValueTask.FromResult<Term>(Term.A(BehaviourAtoms.Ok));
            }
            );
            Add(
                OtpNames.GenServer,
                OtpNames.Stop,
                1,
                (c, a) => GenServer.Stop(c, Resolve(c, a[0], true)!, Term.A(ProcessExitReasons.Normal))
            );
            Add(
                OtpNames.GenServer,
                OtpNames.Stop,
                3,
                (c, a) => GenServer.Stop(
                    c,
                    Resolve(c, a[0], true)!,
                    a[1],
                    Timeout(a[2])
                )
            );
            Add(
                OtpNames.Supervisor,
                OtpNames.StartLink,
                2,
                (c, a) => StartSupervisor(c, a, false)
            );
            Add(
                OtpNames.Supervisor,
                OtpNames.StartLink,
                3,
                (c, a) => StartSupervisor(c, a, true)
            );
            Add(
                OtpNames.Supervisor,
                OtpNames.WhichChildren,
                1,
                (c, a) => GenServer.Call(c, Resolve(c, a[0], true)!, Term.A(OtpNames.WhichChildren))
            );
            Add(
                OtpNames.Supervisor,
                OtpNames.CountChildren,
                1,
                (c, a) => GenServer.Call(c, Resolve(c, a[0], true)!, Term.A(OtpNames.CountChildren))
            );
            Add(
                OtpNames.Supervisor,
                OtpNames.CheckChildSpecs,
                1,
                (c, a) =>
            {
                try
                {
                    if (a[0] is not Cons and not Nil)
                        return ValueTask.FromResult<Term>(Term.Tuple(Term.A(BehaviourAtoms.Error), Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), a[0])));
                    ErlangSupervisor.ParseChildren(a[0]);

                    return ValueTask.FromResult<Term>(Term.A(BehaviourAtoms.Ok));
                }
                catch (ErlangException ex)
                {
                    return ValueTask.FromResult<Term>(Term.Tuple(Term.A(BehaviourAtoms.Error), ex.Reason));
                }
            }
            );
            Installed.Add(registry, new());
        }
    }

    private static string Module(Term value) => value is Atom module ? module.Name : throw new ErlangException(ErlangErrorReasons.BadArgument);

    private static string Name(Term value) => value is TupleTerm { Items.Count: 2 } local && local.Items[0] is Atom { Name: BehaviourAtoms.Local } && local.Items[1] is Atom name ? name.Name : throw new ErlangException(ErlangErrorReasons.BadArgument);

    private static ValueTask<Term> StartServer(
        ProcessContext context,
        IReadOnlyList<Term> args,
        bool link,
        bool named
    )
    {
        int offset = named ? 1 : 0;
        if (args[offset + 2] is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return GenServer.StartModule(
            context,
            Module(args[offset]),
            args[offset + 1],
            link,
            named ? Name(args[0]) : null
        );
    }

    private static async ValueTask<Term> StartSupervisor(ProcessContext context, IReadOnlyList<Term> args, bool named)
    {
        int offset = named ? 1 : 0;
        string module = Module(args[offset]);
        string? name = named ? Name(args[0]) : null;
        try
        {
            var handle = await Supervisor.StartModule(
                context.Runtime,
                module,
                args[offset + 1],
                context,
                name
            );

            return Term.Tuple(Term.A(BehaviourAtoms.Ok), handle.Process.Pid);
        }
        catch (ErlangException ex)
        {
            return ex.Reason is Atom { Name: BehaviourAtoms.Ignore } ? ex.Reason : Term.Tuple(Term.A(BehaviourAtoms.Error), ex.Reason);
        }
    }

    private static Pid? Resolve(ProcessContext context, Term value, bool required)
    {
        Pid? pid = value switch
        {
            Pid direct => direct,
            Atom name => context.Runtime.WhereIs(name.Name) as Pid,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };
        if (required && pid is null)
            throw new ErlangException(Term.A(ErlangErrorReasons.NoProcess), ErlangExceptionClasses.Exit);

        return pid;
    }

    private static TimeSpan? Timeout(Term value) => value switch
    {
        Atom { Name: BehaviourAtoms.Infinity } => null,
        Integer n when n.Value >= 0 && n.Value <= int.MaxValue => TimeSpan.FromMilliseconds((int)n.Value),
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    };

    private static async ValueTask<Term> Call(ProcessContext context, IReadOnlyList<Term> args)
    {
        TimeSpan? timeout = args.Count == 2 ? TimeSpan.FromMilliseconds(BehaviourDefaults.CallTimeoutMilliseconds) : Timeout(args[2]);
        try
        {
            return await GenServer.Call(
                context,
                Resolve(context, args[0], true)!,
                args[1],
                timeout,
                timeout is null
            );
        }
        catch (ErlangException ex) when (ex.ExceptionClass == ErlangExceptionClasses.Exit)
        {
            throw new ErlangException(
                Term.Tuple(ex.Reason, Term.Tuple(Term.A(OtpNames.GenServer), Term.A(OtpNames.Call), Cons.From(args))),
                ErlangExceptionClasses.Exit
            );
        }
    }

    private static IReadOnlyList<ChildSpec> Visible(SupervisorHandle handle) => handle.Specifications.Where(s => s.Restart != RestartPolicy.Temporary || handle.Children.Any(c => c.Id.Equals(s.Id))).ToArray();

    internal static Term WhichChildren(SupervisorHandle handle) => Term.List(Visible(handle).Reverse().Select(s => Term.Tuple(
        s.Id,
        handle.Children.FirstOrDefault(c => c.Id.Equals(s.Id)).Pid ?? (Term)Term.A(SupervisorAtoms.Undefined),
        s.Type ?? Term.A(SupervisorAtoms.Worker),
        s.Modules ?? Nil.Value
    )).ToArray());

    internal static Term CountChildren(SupervisorHandle handle)
    {
        var specs = Visible(handle);

        return Term.List(
            Term.Tuple(Term.A(SupervisorAtoms.Specs), Term.I(specs.Count)),
            Term.Tuple(Term.A(SupervisorAtoms.Active), Term.I(handle.Children.Count)),
            Term.Tuple(Term.A(SupervisorAtoms.Supervisors), Term.I(specs.Count(s => s.Type is Atom { Name: SupervisorAtoms.Supervisor }))),
            Term.Tuple(Term.A(SupervisorAtoms.Workers), Term.I(specs.Count(s => s.Type is not Atom { Name: SupervisorAtoms.Supervisor })))
        );
    }
}
