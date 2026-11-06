// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP child lifecycle, monitor-based shutdown and dynamic shutdown.
namespace Erlang;

public static partial class SupervisorModule
{
    private static async ValueTask<TupleTerm> StartChild(
        ProcessContext context,
        SupervisorState state,
        SupervisorChild child,
        string? progressLevel
    )
    {
        Term result;
        try
        {
            using var source = SupervisorSource.Scope(
                context,
                SupervisorSourceNames.DoStartChildInternal,
                3,
                SupervisorSourceLines.ApplyChild
            );
            result = await Call(
                context,
                SupervisorProtocol.Erlang,
                SupervisorProtocol.Apply,
                child.Start.Items.ToArray()
            );
        }
        catch (ErlangException error) when (error.ExceptionClass == SupervisorProtocol.Throw)
        {
            result = error.Reason;
        }
        catch (ErlangException error)
        {
            return (TupleTerm)Error(T(
                A(SupervisorProtocol.LinkedExit),
                error.ExceptionClass == SupervisorProtocol.ExitSignal ? error.Reason : T(error.Reason, error.StackTraceTerm)
            ));
        }
        if (Is(result, SupervisorProtocol.Ignore))
        {
            return T(A(SupervisorProtocol.Ok), A(SupervisorProtocol.Undefined));
        }
        if (result is TupleTerm { Items.Count: 2 } failure && Is(failure.Items[0], SupervisorProtocol.Error))
        {
            return failure;
        }
        if (result is TupleTerm success && success.Items.Count is 2 or 3 && Is(success.Items[0], SupervisorProtocol.Ok) && success.Items[1] is Pid)
        {
            if (progressLevel is not null)
            {
                await Report(
                    context,
                    state,
                    SupervisorProtocol.Progress,
                    A(SupervisorProtocol.Undefined),
                    child with { Pid = success.Items[1] },
                    progressLevel
                );
            }

            return success;
        }

        return (TupleTerm)Error(result);
    }

    private static async ValueTask<Term?> StartChildren(ProcessContext context, SupervisorState state, SupervisorChild[] startup)
    {
        var ordered = new List<SupervisorChild>();
        for (int i = 0; i < startup.Length; i++)
        {
            SupervisorChild child = startup[i];
            TupleTerm result = await StartChild(
                context,
                state,
                child,
                SupervisorProtocol.Info
            );
            if (Is(result.Items[0], SupervisorProtocol.Error))
            {
                await Report(
                    context,
                    state,
                    SupervisorProtocol.StartError,
                    result.Items[1],
                    child,
                    startup: true
                );
                // children_map reverses the entire unvisited tail on an abort.
                state.Children.Clear();
                state.Children.AddRange(startup.Skip(i + 1).Reverse());
                state.Children.Add(child);
                state.Children.AddRange(ordered);

                return T(A(SupervisorProtocol.FailedToStartChild), child.Id, result.Items[1]);
            }
            if (!child.Temporary || result.Items[1] is Pid)
            {
                ordered.Insert(0, child with { Pid = result.Items[1] });
            }
        }
        state.Children.Clear();
        state.Children.AddRange(ordered);

        return null;
    }

    private static void SetChild(SupervisorState state, SupervisorChild child)
    {
        int index = state.Children.FindIndex(c => c.Id.Equals(child.Id));
        state.Children[index] = child;
    }

    private static void DeleteChild(SupervisorState state, SupervisorChild child)
    {
        if (state.Simple)
        {
            state.Dynamics.Remove(child.Pid);
        }
        else if (child.Temporary)
        {
            state.Children.RemoveAll(c => c.Id.Equals(child.Id));
        }
        else
        {
            SetChild(state, child with { Pid = A(SupervisorProtocol.Undefined) });
        }
    }

    private static SupervisorChild? FindDynamic(SupervisorState state, Term pid, bool arguments)
    {
        if (!state.Dynamics.TryGetValue(pid, out Term? args))
        {
            return null;
        }
        SupervisorChild child = state.Children[0] with { Pid = pid };

        return arguments ? child with
        {
            Start = T(
                child.Start.Items[0],
                child.Start.Items[1],
                Is(state.DynamicKind, SupervisorProtocol.Mapsets) ? A(SupervisorProtocol.Undefined) : args
            )
        } : child;
    }

    private static async ValueTask<SupervisorChild?> FindChild(ProcessContext context, SupervisorState state, Term id)
    {
        if (!state.Simple || id is not Pid)
        {
            return state.Children.FirstOrDefault(c => c.Id.Equals(id));
        }
        SupervisorChild? child = FindDynamic(state, id, false) ?? FindDynamic(state, RestartPid(id), false);
        if (child is not null)
        {
            return child;
        }
        Term alive = await Call(
            context,
            SupervisorProtocol.Erlang,
            SupervisorProtocol.IsProcessAlive,
            id
        );

        return Is(alive, SupervisorProtocol.False) ? state.Children[0] : null;
    }

    private static void StoreDynamic(SupervisorState state, Term pid, Term args)
        => state.Dynamics[pid] = Is(state.DynamicKind, SupervisorProtocol.Mapsets) ? Nil.Value : args;

    private static async ValueTask<Term> UnlinkFlush(ProcessContext context, Term pid, Term reason)
    {
        TupleTerm? Exit(Term message) => message is TupleTerm { Items.Count: 3 } exit && Is(exit.Items[0], SupervisorProtocol.LinkedExit) && exit.Items[1].Equals(pid) ? exit : null;
        if (Is(reason, SupervisorProtocol.Noproc))
        {
            var info = (TupleTerm)await Call(
                context,
                SupervisorProtocol.Erlang,
                SupervisorProtocol.ProcessInfo,
                context.Self,
                A(SupervisorProtocol.Links)
            );
            bool linked = Items(info.Items[1]).Contains(pid);
            TupleTerm? exit = await context.ReceiveAsync(Exit, linked ? null : TimeSpan.Zero);

            return exit?.Items[2] ?? A(SupervisorProtocol.ChildProcessUnlinked);
        }
        await Call(
            context,
            SupervisorProtocol.Erlang,
            SupervisorProtocol.Unlink,
            pid
        );
        await context.ReceiveAsync(Exit, TimeSpan.Zero);

        return reason;
    }

    private static bool ExpectedShutdown(SupervisorChild child, Term reason, bool brutal)
        => Is(reason, brutal ? SupervisorProtocol.Killed : SupervisorProtocol.Shutdown) || !Is(child.Restart, SupervisorProtocol.Permanent)
            && (Is(reason, SupervisorProtocol.Normal, SupervisorProtocol.Shutdown) || reason is TupleTerm { Items.Count: 2 } pair && Is(pair.Items[0], SupervisorProtocol.Shutdown));

    private static async ValueTask TerminateChild(ProcessContext context, SupervisorState state, SupervisorChild child)
    {
        if (child.Pid is not Pid)
        {
            return;
        }
        Term monitor = await Call(
            context,
            SupervisorProtocol.Erlang,
            SupervisorProtocol.Monitor,
            A(SupervisorProtocol.Process),
            child.Pid
        );
        bool brutal = Is(child.Shutdown, SupervisorProtocol.BrutalKill);
        await Call(
            context,
            SupervisorProtocol.Erlang,
            SupervisorProtocol.ExitSignal,
            child.Pid,
            A(brutal ? SupervisorProtocol.Kill : SupervisorProtocol.Shutdown)
        );

        TupleTerm? Down(Term message) => message is TupleTerm { Items.Count: 5 } down && Is(down.Items[0], SupervisorProtocol.DOWN)
            && down.Items[1].Equals(monitor) && down.Items[3].Equals(child.Pid) ? down : null;
        TupleTerm? result = await Receive(context, Down, brutal ? A(SupervisorProtocol.Infinity) : child.Shutdown);
        if (result is null)
        {
            await Call(
                context,
                SupervisorProtocol.Erlang,
                SupervisorProtocol.ExitSignal,
                child.Pid,
                A(SupervisorProtocol.Kill)
            );
            result = await context.ReceiveAsync(Down);
        }
        Term reason = await UnlinkFlush(context, child.Pid, result!.Items[4]);
        if (!ExpectedShutdown(child, reason, brutal))
        {
            await Report(
                context,
                state,
                SupervisorProtocol.ShutdownError,
                reason,
                child
            );
        }
    }

    private static async ValueTask<T?> Receive<T>(ProcessContext context, Func<Term, T?> match, Term timeout) where T : class
    {
        // Preserve the VM's arbitrary-size timeout validation rather than narrowing to Int32.
        if (Is(timeout, SupervisorProtocol.Infinity))
        {
            return await context.ReceiveAsync(match);
        }
        var value = ((Integer)timeout).Value;
        if (value < 0 || value > uint.MaxValue)
        {
            throw new ErlangException(SupervisorProtocol.TimeoutValue);
        }

        return await context.ReceiveAsync(match, TimeSpan.FromMilliseconds((double)value));
    }

    private static async ValueTask TerminateChildren(ProcessContext context, SupervisorState state, SupervisorChild[] shutdown)
    {
        var remaining = new List<SupervisorChild>();
        foreach (SupervisorChild child in shutdown)
        {
            await TerminateChild(context, state, child);
            if (!child.Temporary)
            {
                remaining.Insert(0, child with { Pid = A(SupervisorProtocol.Undefined) });
            }
        }
        state.Children.Clear();
        state.Children.AddRange(remaining);
    }

    private static async ValueTask TerminateDynamic(ProcessContext context, SupervisorState state)
    {
        SupervisorChild child = state.Children[0];
        bool brutal = Is(child.Shutdown, SupervisorProtocol.BrutalKill);
        var pending = new Dictionary<Term, Term>();
        foreach (Term pid in state.Dynamics.Keys.Where(pid => pid is Pid))
        {
            Term monitor = await Call(
                context,
                SupervisorProtocol.Erlang,
                SupervisorProtocol.Monitor,
                A(SupervisorProtocol.Process),
                pid
            );
            await Call(
                context,
                SupervisorProtocol.Erlang,
                SupervisorProtocol.ExitSignal,
                pid,
                A(brutal ? SupervisorProtocol.Kill : SupervisorProtocol.Shutdown)
            );
            pending.Add(pid, monitor);
        }
        Term timer = !brutal && !Is(child.Shutdown, SupervisorProtocol.Infinity)
            ? await Call(
                context,
                SupervisorProtocol.Erlang,
                SupervisorProtocol.StartTimer,
                child.Shutdown,
                context.Self,
                A(SupervisorProtocol.Kill)
            ) : A(SupervisorProtocol.Undefined);
        var errors = new Dictionary<Term, List<Term>>();
        while (pending.Count > 0)
        {
            TupleTerm? Select(Term message)
            {
                if (message is TupleTerm { Items.Count: 5 } down && Is(down.Items[0], SupervisorProtocol.DOWN)
                    && pending.TryGetValue(down.Items[3], out Term? monitor) && monitor.Equals(down.Items[1]))
                {
                    return down;
                }

                return message is TupleTerm { Items.Count: 3 } expired && Is(expired.Items[0], SupervisorProtocol.Timeout) && expired.Items[1].Equals(timer) ? expired : null;
            }
            TupleTerm message = (await context.ReceiveAsync(Select))!;
            if (message.Items.Count == 3)
            {
                foreach (Term pid in pending.Keys)
                {
                    await Call(
                        context,
                        SupervisorProtocol.Erlang,
                        SupervisorProtocol.ExitSignal,
                        pid,
                        A(SupervisorProtocol.Kill)
                    );
                }
                timer = A(SupervisorProtocol.Undefined);
                continue;
            }
            Term ended = message.Items[3];
            Term reason = await UnlinkFlush(context, ended, message.Items[4]);
            pending.Remove(ended);
            if (!ExpectedShutdown(child, reason, brutal))
            {
                if (!errors.TryGetValue(reason, out List<Term>? pids))
                {
                    errors[reason] = pids = [];
                }
                pids.Insert(0, ended);
            }
        }
        if (!Is(timer, SupervisorProtocol.Undefined))
        {
            await Call(
                context,
                SupervisorProtocol.Erlang,
                SupervisorProtocol.CancelTimer,
                timer
            );
            await context.ReceiveAsync(
                message => message is TupleTerm { Items.Count: 3 } expired && Is(expired.Items[0], SupervisorProtocol.Timeout) && expired.Items[1].Equals(timer) ? expired : null,
                TimeSpan.Zero
            );
        }
        foreach (var error in errors)
        {
            await Report(
                context,
                state,
                SupervisorProtocol.ShutdownError,
                error.Key,
                child with { Pid = Cons.From(error.Value) }
            );
        }
    }
}
