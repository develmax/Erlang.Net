// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP-29.1.1 supervisor gen_server callbacks.
namespace Erlang;

public static partial class SupervisorModule
{
    private static async ValueTask Wakeup(ProcessContext context, SupervisorState state, bool hibernate = false)
    {
        if (hibernate || Is(state.Hibernating, SupervisorProtocol.True))
        {
            Term[] restarts = Items(state.Restarts);
            if (restarts.Length > 0)
            {
                var now = (Integer)await Call(
                    context,
                    SupervisorProtocol.Erlang,
                    SupervisorProtocol.MonotonicTime,
                    A(SupervisorProtocol.Second)
                );
                if (((Integer)restarts[0]).Value < now.Value - ((Integer)state.Period).Value)
                {
                    state.Restarts = Nil.Value;
                    state.NRestarts = Term.I(0);
                }
            }
            state.Hibernating = A(hibernate ? SupervisorProtocol.True : SupervisorProtocol.False);
        }
    }

    private static async ValueTask<Term> HandleCall(ProcessContext context, Term message, SupervisorState state)
    {
        await Wakeup(context, state);
        if (Is(message, SupervisorProtocol.WhichChildren))
        {
            IEnumerable<SupervisorChild> children = state.Simple
                ? new MapTerm(state.Dynamics).Entries.Select(entry => state.Children[0] with { Id = A(SupervisorProtocol.Undefined), Pid = entry.Key })
                : state.Children;

            return Reply(Cons.From(children.Select(ChildInfo)), state);
        }
        if (Is(message, SupervisorProtocol.CountChildren))
        {
            int active = 0;
            int supervisors = 0;
            int workers = 0;
            int specs = state.Simple ? 1 : state.Children.Count;
            if (state.Simple)
            {
                active = state.Dynamics.Count - (int)((Integer)state.DynamicRestarts).Value;
                if (Is(state.Children[0].Type, SupervisorProtocol.Supervisor))
                {
                    supervisors = state.Dynamics.Count;
                }
                else
                {
                    workers = state.Dynamics.Count;
                }
            }
            else
            {
                foreach (SupervisorChild countedChild in state.Children)
                {
                    if (Is(countedChild.Type, SupervisorProtocol.Supervisor))
                    {
                        supervisors++;
                    }
                    else
                    {
                        workers++;
                    }
                    if (countedChild.Pid is Pid && Is(
                        await Call(
                            context,
                            SupervisorProtocol.Erlang,
                            SupervisorProtocol.IsProcessAlive,
                            countedChild.Pid
                        ),
                        SupervisorProtocol.True
                    ))
                    {
                        active++;
                    }
                }
            }

            return Reply(
                Term.List(
                    T(A(SupervisorProtocol.Specs), Term.I(specs)),
                    T(A(SupervisorProtocol.Active), Term.I(active)),
                    T(A(SupervisorProtocol.Supervisors), Term.I(supervisors)),
                    T(A(SupervisorProtocol.Workers), Term.I(workers))
                ),
                state
            );
        }
        var request = Tuple(message, 2).Items;
        string operation = request[0] is Atom atom ? atom.Name : throw new ErlangException(SupervisorProtocol.FunctionClause);
        Term id = request[1];
        if (operation is not (SupervisorProtocol.StartChild or SupervisorProtocol.RestartChild or SupervisorProtocol.DeleteChild or SupervisorProtocol.TerminateChild or SupervisorProtocol.WhichChild or SupervisorProtocol.GetChildspec))
        {
            throw new ErlangException(SupervisorProtocol.FunctionClause);
        }
        if (operation == SupervisorProtocol.StartChild)
        {
            if (state.Simple)
            {
                SupervisorChild template = state.Children[0];
                Term args = id;
                foreach (Term item in Items(template.Start.Items[2]).Reverse())
                {
                    args = new Cons(item, args);
                }
                TupleTerm result = await StartChild(
                    context,
                    state,
                    template with { Start = T(template.Start.Items[0], template.Start.Items[1], args) },
                    null
                );
                if (Is(result.Items[0], SupervisorProtocol.Ok) && result.Items[1] is Pid)
                {
                    StoreDynamic(state, result.Items[1], args);
                }

                return Reply(result, state);
            }
            SupervisorChild specification;
            try
            {
                specification = ParseChild(id, state.AutoShutdown);
            }
            catch (ErlangException error) when (error.ExceptionClass == SupervisorProtocol.Throw)
            {
                return Reply(Error(error.Reason), state);
            }
            SupervisorChild? previous = state.Children.FirstOrDefault(c => c.Id.Equals(specification.Id));
            if (previous is not null)
            {
                return Reply(
                    Error(previous.Pid is Pid ? T(A(SupervisorProtocol.AlreadyStarted), previous.Pid) : A(SupervisorProtocol.AlreadyPresent)),
                    state
                );
            }
            TupleTerm started = await StartChild(
                context,
                state,
                specification,
                SupervisorProtocol.Debug
            );
            if (Is(started.Items[0], SupervisorProtocol.Error))
            {
                Term reason = started.Items[1];

                return Reply(
                    reason is TupleTerm { Items.Count: 2 } pair && Is(pair.Items[0], SupervisorProtocol.AlreadyStarted)
                    ? started : Error(T(reason, specification.ToTerm())),
                    state
                );
            }
            if (!specification.Temporary || started.Items[1] is Pid)
            {
                if (specification.Temporary)
                {
                    specification = specification with { Start = T(specification.Start.Items[0], specification.Start.Items[1], A(SupervisorProtocol.Undefined)) };
                }
                state.Children.Insert(0, specification with { Pid = started.Items[1] });
            }

            return Reply(started, state);
        }
        if (state.Simple && (operation is SupervisorProtocol.RestartChild or SupervisorProtocol.DeleteChild || operation is SupervisorProtocol.TerminateChild or SupervisorProtocol.WhichChild && id is not Pid))
        {
            return Reply(Error(A(SupervisorProtocol.SimpleOneForOne)), state);
        }
        // which_child deliberately does not apply the dead-Pid compatibility rule.
        SupervisorChild? child = state.Simple && operation == SupervisorProtocol.WhichChild ? FindDynamic(state, id, false)
            : await FindChild(context, state, id);
        if (child is null)
        {
            return Reply(Error(A(SupervisorProtocol.NotFound)), state);
        }
        switch (operation)
        {
            case SupervisorProtocol.GetChildspec:
                return Reply(T(A(SupervisorProtocol.Ok), child.ToSpec()), state);
            case SupervisorProtocol.WhichChild:
                return Reply(T(A(SupervisorProtocol.Ok), ChildInfo(state.Simple ? child with { Id = A(SupervisorProtocol.Undefined) } : child)), state);
            case SupervisorProtocol.TerminateChild:
                await TerminateChild(context, state, child);
                DeleteChild(state, child);

                return Reply(A(SupervisorProtocol.Ok), state);
            case SupervisorProtocol.DeleteChild:
                if (!Is(child.Pid, SupervisorProtocol.Undefined))
                {
                    return Reply(Error(A(Restarting(child.Pid) ? SupervisorProtocol.Restarting : SupervisorProtocol.Running)), state);
                }
                state.Children.RemoveAll(c => c.Id.Equals(id));

                return Reply(A(SupervisorProtocol.Ok), state);
            case SupervisorProtocol.RestartChild:
                if (!Is(child.Pid, SupervisorProtocol.Undefined))
                {
                    return Reply(Error(A(Restarting(child.Pid) ? SupervisorProtocol.Restarting : SupervisorProtocol.Running)), state);
                }
                TupleTerm started = await StartChild(
                    context,
                    state,
                    child,
                    SupervisorProtocol.Debug
                );
                if (Is(started.Items[0], SupervisorProtocol.Ok))
                {
                    SetChild(state, child with { Pid = started.Items[1] });
                }

                return Reply(started, state);
            default:
                throw new ErlangException(SupervisorProtocol.FunctionClause);
        }
    }

    private static Term ChildInfo(SupervisorChild child) => T(
        child.Id,
        Restarting(child.Pid) ? A(SupervisorProtocol.Restarting) : child.Pid,
        child.Type,
        child.Modules
    );

    private static async ValueTask<Term> HandleCast(ProcessContext context, Term message, SupervisorState state)
    {
        await Wakeup(context, state);
        if (message is not TupleTerm { Items.Count: 3 } retry || !Is(retry.Items[0], SupervisorProtocol.TryAgainRestart) || !retry.Items[1].Equals(state.Tag))
        {
            throw new ErlangException(SupervisorProtocol.FunctionClause);
        }
        Term id = retry.Items[2];
        SupervisorChild? child = state.Simple ? FindDynamic(state, id, true)
            : id is Pid ? state.Children.FirstOrDefault(c => c.Pid.Equals(id)) : state.Children.FirstOrDefault(c => c.Id.Equals(id));
        bool stop = child is not null && Restarting(child.Pid) && await Restart(context, state, child);

        return NoReply(state, stop);
    }

    private static async ValueTask<Term> HandleInfo(ProcessContext context, Term message, SupervisorState state)
    {
        if (message is TupleTerm { Items.Count: 2 } hibernate && Is(hibernate.Items[0], SupervisorProtocol.Hibernate) && hibernate.Items[1].Equals(state.Tag))
        {
            await Wakeup(context, state, true);

            return T(A(SupervisorProtocol.Noreply), state.ToTerm(), A(SupervisorProtocol.Hibernate));
        }
        await Wakeup(context, state);
        if (message is TupleTerm { Items.Count: 3 } exit && Is(exit.Items[0], SupervisorProtocol.LinkedExit))
        {
            Term pid = exit.Items[1];
            SupervisorChild? child = state.Simple ? FindDynamic(state, pid, true) : state.Children.FirstOrDefault(c => c.Pid.Equals(pid));
            if (child is null)
            {
                return NoReply(state);
            }
            Term reason = exit.Items[2];
            bool normal = Is(reason, SupervisorProtocol.Normal, SupervisorProtocol.Shutdown) || reason is TupleTerm { Items.Count: 2 } tuple && Is(tuple.Items[0], SupervisorProtocol.Shutdown);
            if (Is(child.Restart, SupervisorProtocol.Permanent) || !normal && Is(child.Restart, SupervisorProtocol.Transient))
            {
                await Report(
                    context,
                    state,
                    SupervisorProtocol.ChildTerminated,
                    reason,
                    child
                );

                return NoReply(state, await Restart(context, state, child));
            }
            if (child.Temporary)
            {
                await Report(
                    context,
                    state,
                    SupervisorProtocol.ChildTerminated,
                    reason,
                    child
                );
            }
            DeleteChild(state, child);
            bool significant = Is(child.Significant, SupervisorProtocol.True) && !Is(state.AutoShutdown, SupervisorProtocol.Never);
            bool stop = significant && (Is(state.AutoShutdown, SupervisorProtocol.AnySignificant) || !state.Children.Any(c => Is(c.Significant, SupervisorProtocol.True) && !Is(c.Pid, SupervisorProtocol.Undefined)));

            return NoReply(state, stop);
        }
        await UnexpectedMessage(context, message);

        return NoReply(state);
    }
}
