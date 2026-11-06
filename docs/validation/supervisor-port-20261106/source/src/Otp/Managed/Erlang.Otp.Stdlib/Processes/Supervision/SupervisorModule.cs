// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP-29.1.1 supervisor translated to C# using runtime terms and processes.
using System.Numerics;

namespace Erlang;

public static partial class SupervisorModule
{
    public const string Name = SupervisorProtocol.Supervisor;

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (SupervisorProtocol.StartLink, 2), (SupervisorProtocol.StartLink, 3), (SupervisorProtocol.StartChild, 2), (SupervisorProtocol.RestartChild, 2),
        (SupervisorProtocol.DeleteChild, 2), (SupervisorProtocol.TerminateChild, 2), (SupervisorProtocol.WhichChildren, 1), (SupervisorProtocol.WhichChild, 2),
        (SupervisorProtocol.CountChildren, 1), (SupervisorProtocol.CheckChildspecs, 1), (SupervisorProtocol.CheckChildspecs, 2), (SupervisorProtocol.GetChildspec, 2),
        (SupervisorProtocol.Stop, 1), (SupervisorProtocol.Stop, 3), (SupervisorProtocol.Init, 1), (SupervisorProtocol.HandleCall, 3), (SupervisorProtocol.HandleCast, 2),
        (SupervisorProtocol.HandleInfo, 2), (SupervisorProtocol.Terminate, 2), (SupervisorProtocol.CodeChange, 3), (SupervisorProtocol.FormatLog, 1),
        (SupervisorProtocol.FormatLog, 2), (SupervisorProtocol.GetCallbackModule, 1), (SupervisorProtocol.BehaviourInfo, 1)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallPreloaded(preloaded => preloaded.InstallSourceWrappers(target =>
        {
            foreach (var export in Exports)
            {
                string function = export.Name;
                target.Register(
                    Name,
                    function,
                    export.Arity,
                    (c, a) => Invoke(c, function, a)
                );
            }
        }));

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> a)
    {
        using var frame = context.EnterSourceTailCall(Name, function);
        if (!Exports.Contains((function, a.Count)))
        {
            throw new ErlangException(SupervisorProtocol.Undef);
        }
        switch (function)
        {
            case SupervisorProtocol.StartLink:
                return a.Count == 2
                    ? await Call(
                        context,
                        SupervisorProtocol.GenServer,
                        function,
                        A(Name),
                        T(A(SupervisorProtocol.Self), a[0], a[1]),
                        Nil.Value
                    )
                    : await Call(
                        context,
                        SupervisorProtocol.GenServer,
                        function,
                        a[0],
                        A(Name),
                        T(a[0], a[1], a[2]),
                        Nil.Value
                    );
            case SupervisorProtocol.Stop:
                return await Call(
                    context,
                    SupervisorProtocol.GenServer,
                    function,
                    a.ToArray()
                );
            case SupervisorProtocol.StartChild or SupervisorProtocol.RestartChild or SupervisorProtocol.DeleteChild or SupervisorProtocol.TerminateChild or SupervisorProtocol.WhichChild or SupervisorProtocol.GetChildspec:
                return await Call(
                    context,
                    SupervisorProtocol.GenServer,
                    SupervisorProtocol.Call,
                    a[0],
                    T(A(function), a[1]),
                    A(SupervisorProtocol.Infinity)
                );
            case SupervisorProtocol.WhichChildren or SupervisorProtocol.CountChildren:
                return await Call(
                    context,
                    SupervisorProtocol.GenServer,
                    SupervisorProtocol.Call,
                    a[0],
                    A(function),
                    A(SupervisorProtocol.Infinity)
                );
            case SupervisorProtocol.CheckChildspecs:
                if (!IsList(a[0]))
                {
                    return Error(T(A(SupervisorProtocol.Badarg), a[0]));
                }
                Term automatic = a.Count == 1 ? A(SupervisorProtocol.Undefined) : a[1];
                if (!Is(
                    automatic,
                    SupervisorProtocol.Undefined,
                    SupervisorProtocol.Never,
                    SupervisorProtocol.AnySignificant,
                    SupervisorProtocol.AllSignificant
                ))
                {
                    return Error(T(A(SupervisorProtocol.Badarg), automatic));
                }
                try
                {
                    ParseChildren(a[0], automatic);

                    return A(SupervisorProtocol.Ok);
                }
                catch (ErlangException error) when (error.ExceptionClass == SupervisorProtocol.Throw)
                {
                    return Error(error.Reason);
                }
            case SupervisorProtocol.Init:
                return await Initialize(context, a[0]);
            case SupervisorProtocol.HandleCall:
                return await HandleCall(context, a[0], new SupervisorState(a[2]));
            case SupervisorProtocol.HandleCast:
                return await HandleCast(context, a[0], new SupervisorState(a[1]));
            case SupervisorProtocol.HandleInfo:
                return await HandleInfo(context, a[0], new SupervisorState(a[1]));
            case SupervisorProtocol.Terminate:
                {
                    var state = new SupervisorState(a[1]);
                    if (state.Simple)
                    {
                        await TerminateDynamic(context, state);

                        return A(SupervisorProtocol.Ok);
                    }
                    await TerminateChildren(context, state, state.Children.ToArray());

                    return state.ChildrenTerm();
                }
            case SupervisorProtocol.CodeChange:
                return await CodeChange(context, new SupervisorState(a[1]));
            case SupervisorProtocol.FormatLog:
                return await FormatLog(context, a);
            case SupervisorProtocol.GetCallbackModule:
                {
                    var status = Tuple(await Call(
                        context,
                        SupervisorProtocol.Sys,
                        SupervisorProtocol.GetStatus,
                        a[0]
                    ), 4);
                    Term[] data = Items(status.Items[3]);
                    Term misc = data[4];
                    foreach (Term item in Items(misc))
                    {
                        if (item is TupleTerm { Items.Count: 2 } entry && Is(entry.Items[0], Name))
                        {
                            return Tuple(Items(entry.Items[1])[0], 2).Items[1];
                        }
                    }
                    Term[] sections = Items(misc);
                    Term state = Tuple(Items(Tuple(sections[2], 2).Items[1])[0], 2).Items[1];

                    return new SupervisorState(state).Module;
                }
            case SupervisorProtocol.BehaviourInfo:
                if (Is(a[0], SupervisorProtocol.Callbacks))
                {
                    return Term.List(T(A(SupervisorProtocol.Init), Term.I(1)));
                }
                if (Is(a[0], SupervisorProtocol.OptionalCallbacks))
                {
                    return Nil.Value;
                }
                throw SupervisorSource.FunctionClause(context, SupervisorSourceNames.InlinedBehaviourInfo, a);
            default:
                throw new ErlangException(SupervisorProtocol.FunctionClause);
        }
    }

    private static Atom A(string name) => Term.A(name);

    private static TupleTerm T(params Term[] terms) => Term.Tuple(terms);

    private static Term Error(Term reason) => T(A(SupervisorProtocol.Error), reason);

    private static bool Is(Term value, params string[] names) => value is Atom atom && names.Contains(atom.Name);

    private static bool IsList(Term value) => value is Nil or Cons;

    private static Term[] Items(Term value) => Cons.Items(value).ToArray();

    private static TupleTerm Tuple(Term value, int size) => value is TupleTerm tuple && tuple.Items.Count == size
        ? tuple : throw new ErlangException(SupervisorProtocol.FunctionClause);

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

    private static Term Get(MapTerm map, string name, Term fallback)
        => map.TryGet(A(name), out Term? value) ? value! : fallback;

    private static void Invalid(string reason, Term value) => throw new ErlangException(T(A(reason), value), SupervisorProtocol.Throw);

    private static bool Restarting(Term pid) => pid is TupleTerm { Items.Count: 2 } tuple && Is(tuple.Items[0], SupervisorProtocol.Restarting);

    private static Term RestartPid(Term pid) => pid is Pid ? T(A(SupervisorProtocol.Restarting), pid) : pid;

    private static Term Action(SupervisorState state) => T(A(SupervisorProtocol.Timeout), state.HibernateAfter, T(A(SupervisorProtocol.Hibernate), state.Tag));

    private static Term Reply(Term value, SupervisorState state) => T(
        A(SupervisorProtocol.Reply),
        value,
        state.ToTerm(),
        Action(state)
    );

    private static Term NoReply(SupervisorState state, bool stop = false) => stop
        ? T(A(SupervisorProtocol.Stop), A(SupervisorProtocol.Shutdown), state.ToTerm())
        : T(A(SupervisorProtocol.Noreply), state.ToTerm(), Action(state));
}
