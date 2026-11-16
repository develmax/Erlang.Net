// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP restart intensity and strategy algorithms.
namespace Erlang;

public static partial class SupervisorModule
{
    private static async ValueTask<bool> AddRestart(ProcessContext context, SupervisorState state)
    {
        var intensity = ((Integer)state.Intensity).Value;
        if (intensity.IsZero)
        {
            return false;
        }
        Term now = await Call(
            context,
            SupervisorProtocol.Erlang,
            SupervisorProtocol.MonotonicTime,
            A(SupervisorProtocol.Second)
        );
        var n = ((Integer)state.NRestarts).Value;
        if (n < System.Numerics.BigInteger.Min(intensity, SupervisorDefaults.DirtyRestartLimit))
        {
            state.Restarts = new Cons(now, state.Restarts);
            state.NRestarts = new Integer(n + 1);

            return true;
        }
        var threshold = ((Integer)now).Value - ((Integer)state.Period).Value;
        var kept = new List<Term>();
        bool allowed = true;
        foreach (Term restart in Items(state.Restarts))
        {
            if (((Integer)restart).Value < threshold)
            {
                break;
            }
            if (kept.Count == intensity - 1)
            {
                allowed = false;
                break;
            }
            kept.Add(restart);
        }
        state.Restarts = allowed ? new Cons(now, Cons.From(kept)) : Cons.From(kept);
        state.NRestarts = Term.I(kept.Count + (allowed ? 1 : 0));

        return allowed;
    }

    private static async ValueTask<bool> Restart(ProcessContext context, SupervisorState state, SupervisorChild child)
    {
        if (!await AddRestart(context, state))
        {
            await Report(
                context,
                state,
                SupervisorProtocol.Shutdown,
                A(SupervisorProtocol.ReachedMaxRestartIntensity),
                child
            );
            DeleteChild(state, child);

            return true;
        }
        Term? again = null;
        if (state.Simple)
        {
            if (Restarting(child.Pid))
            {
                state.DynamicRestarts = new Integer(((Integer)state.DynamicRestarts).Value - 1);
            }
            state.Dynamics.Remove(child.Pid);
            TupleTerm result = await StartChild(
                context,
                state,
                child,
                null
            );
            if (Is(result.Items[0], SupervisorProtocol.Ok))
            {
                if (result.Items[1] is Pid)
                {
                    StoreDynamic(state, result.Items[1], child.Start.Items[2]);
                }
            }
            else
            {
                again = RestartPid(child.Pid);
                StoreDynamic(state, again, child.Start.Items[2]);
                state.DynamicRestarts = new Integer(((Integer)state.DynamicRestarts).Value + 1);
                await Report(
                    context,
                    state,
                    SupervisorProtocol.StartError,
                    result.Items[1],
                    child
                );
            }
        }
        else if (Is(state.Strategy, SupervisorProtocol.OneForOne))
        {
            TupleTerm result = await StartChild(
                context,
                state,
                child,
                SupervisorProtocol.Info
            );
            if (Is(result.Items[0], SupervisorProtocol.Ok))
            {
                SetChild(state, child with { Pid = result.Items[1] });
            }
            else
            {
                SetChild(state, child with { Pid = RestartPid(child.Pid) });
                again = child.Id;
                await Report(
                    context,
                    state,
                    SupervisorProtocol.StartError,
                    result.Items[1],
                    child
                );
            }
        }
        else
        {
            int end = Is(state.Strategy, SupervisorProtocol.OneForAll) ? state.Children.Count : state.Children.FindIndex(c => c.Id.Equals(child.Id)) + 1;
            SupervisorChild[] shutdown = state.Children.Take(end).ToArray();
            SupervisorChild[] before = state.Children.Skip(end).ToArray();
            shutdown = shutdown.Select(c => c.Id.Equals(child.Id) ? c with { Pid = A(SupervisorProtocol.Undefined) } : c).ToArray();
            await TerminateChildren(context, state, shutdown);
            SupervisorChild[] startup = state.Children.ToArray();
            Term? failure = await StartChildren(context, state, startup);
            if (failure is TupleTerm failed)
            {
                Term failedId = failed.Items[1];
                SupervisorChild failedChild = state.Children.First(c => c.Id.Equals(failedId));
                Term pid = failedId.Equals(child.Id) ? RestartPid(child.Pid) : T(A(SupervisorProtocol.Restarting), A(SupervisorProtocol.Undefined));
                SetChild(state, failedChild with { Pid = pid });
                again = failedId;
            }
            state.Children.AddRange(before);
        }
        if (again is not null)
        {
            await Call(
                context,
                SupervisorProtocol.GenServer,
                SupervisorProtocol.Cast,
                context.Self,
                T(A(SupervisorProtocol.TryAgainRestart), state.Tag, again)
            );
        }

        return false;
    }
}
