using System.Diagnostics;

namespace Erlang.Otp;

internal static class SimpleSupervisor
{
    private sealed record Entry(ChildSpec Specification, Pid LastPid);

    internal static async ValueTask<Term> Run(
        ProcessContext context,
        SupervisorConfiguration configuration,
        ProcessContext? parent,
        StartupHandshake startup,
        SupervisorHandle handle
    )
    {
        var runtime = context.Runtime;
        var template = configuration.Children.Single();
        var children = new Dictionary<Term, Entry>();
        var restarts = new Queue<long>();
        var tag = await runtime.Modules.Call(context, ErlangModuleNames.Module, ErlangModuleNames.MakeReference);

        Term Error(string reason) => Term.Tuple(Term.A(BehaviourAtoms.Error), Term.A(reason));

        Term Restarting(Pid pid) => Term.Tuple(Term.A(SupervisorAtoms.Restarting), pid);

        void Publish()
        {
            handle.Specifications = new[] { template };
            handle.Publish(children.Where(pair => pair.Key is Pid).Select(pair => (pair.Key, pair.Value.LastPid)));
            handle.PublishRestarting(children.Keys.Where(key => key is not Pid));
        }

        Term ChildTuple(Term key) => Term.Tuple(
            Term.A(SupervisorAtoms.Undefined),
            key is Pid ? key : Term.A(SupervisorAtoms.Restarting),
            template.Type ?? Term.A(SupervisorAtoms.Worker),
            template.Modules ?? Nil.Value
        );

        Term CountChildren()
        {
            int active = children.Keys.Count(key => key is Pid);
            bool supervisor = template.Type is Atom { Name: SupervisorAtoms.Supervisor };

            return Term.List(
                Term.Tuple(Term.A(SupervisorAtoms.Specs), Term.I(BehaviourDefaults.DynamicTemplateSpecifications)),
                Term.Tuple(Term.A(SupervisorAtoms.Active), Term.I(active)),
                Term.Tuple(Term.A(SupervisorAtoms.Supervisors), Term.I(supervisor ? children.Count : 0)),
                Term.Tuple(Term.A(SupervisorAtoms.Workers), Term.I(supervisor ? 0 : children.Count))
            );
        }

        void Install(ChildSpec specification, TupleTerm result)
        {
            if (result.Items[0] is Atom { Name: BehaviourAtoms.Ok } && result.Items[1] is Pid pid)
                children[pid] = new(specification.Restart == RestartPolicy.Temporary ? template : specification, pid);
            Publish();
        }

        async ValueTask StopChildren(IEnumerable<Pid> pids)
        {
            var pending = new Dictionary<Pid, ReferenceTerm>();
            foreach (Pid pid in pids.Distinct())
            {
                pending[pid] = runtime.Monitor(context, pid);
                runtime.Exit(context, pid, Term.A(template.BrutalKill ? ProcessExitSignals.Kill : ProcessExitReasons.Shutdown));
            }
            TimeSpan? timeout = template.InfiniteShutdown || template.BrutalKill ? null : template.Shutdown ?? TimeSpan.FromMilliseconds(BehaviourDefaults.ChildShutdownMilliseconds);
            long started = Stopwatch.GetTimestamp();

            TupleTerm? Down(Term message)
            {
                if (message is TupleTerm { Items.Count: 5 } down && down.Items[0] is Atom { Name: ProcessMessageTags.MonitorDown } && down.Items[2] is Atom { Name: ProcessMonitorKinds.Process } && down.Items[3] is Pid pid && pending.TryGetValue(pid, out var monitor) && down.Items[1].Equals(monitor))
                    return down;

                return null;
            }

            void Flush(Pid pid, ReferenceTerm monitor)
            {
                runtime.Demonitor(context, monitor, true);
                runtime.Unlink(context, pid);
                context.Mailbox.Remove(message => message is TupleTerm { Items.Count: 3 } exit && exit.Items[0] is Atom { Name: ProcessMessageTags.LinkedExit } && exit.Items[1].Equals(pid));
            }
            try
            {
                while (pending.Count > 0)
                {
                    TimeSpan? remaining = timeout is null ? null : timeout.Value - Stopwatch.GetElapsedTime(started);
                    if (remaining < TimeSpan.Zero)
                        remaining = TimeSpan.Zero;
                    var down = await context.ReceiveAsync(Down, remaining);
                    if (down is null)
                    {
                        foreach (Pid pid in pending.Keys)
                            runtime.Exit(context, pid, Term.A(ProcessExitSignals.Kill));
                        timeout = null;
                        continue;
                    }
                    var finished = (Pid)down.Items[3];
                    Flush(finished, pending[finished]);
                    pending.Remove(finished);
                }
            }
            finally
            {
                foreach (var pair in pending)
                    Flush(pair.Key, pair.Value);
            }
        }

        async ValueTask StopAll()
        {
            var active = children.Keys.OfType<Pid>().ToArray();
            children.Clear();
            Publish();
            await StopChildren(active);
        }

        async ValueTask<Term> Manage(string operation, Term argument)
        {
            if (operation == OtpNames.StartChild)
            {
                var specification = ErlangSupervisor.WithArguments(template, argument);
                var result = (TupleTerm)await specification.StartResult!(context);
                Install(specification, result);

                return result;
            }
            if (operation is OtpNames.RestartChild or OtpNames.DeleteChild)
                return Error(SupervisorAtoms.SimpleOneForOne);
            if (operation == OtpNames.WhichChild)
            {
                if (argument is not Pid)
                    return Error(SupervisorAtoms.SimpleOneForOne);
                Term key = children.ContainsKey(argument) ? argument : Restarting((Pid)argument);

                return children.ContainsKey(key) ? Term.Tuple(Term.A(BehaviourAtoms.Ok), ChildTuple(key)) : Error(SupervisorAtoms.NotFound);
            }
            if (operation == OtpNames.GetChildSpec)
            {
                if (argument.Equals(template.Id) || argument is Pid pid && (children.ContainsKey(pid) || children.ContainsKey(Restarting(pid)) || !runtime.IsAlive(pid)))
                    return Term.Tuple(Term.A(BehaviourAtoms.Ok), ErlangSupervisor.ChildSpecification(template));

                return Error(SupervisorAtoms.NotFound);
            }
            if (argument is not Pid target)
                return Error(SupervisorAtoms.SimpleOneForOne);
            Term selected = children.ContainsKey(target) ? target : Restarting(target);
            if (!children.ContainsKey(selected))
                return runtime.IsAlive(target) ? Error(SupervisorAtoms.NotFound) : Term.A(BehaviourAtoms.Ok);
            if (selected is Pid)
                await StopChildren(new[] { target });
            children.Remove(selected);
            Publish();

            return Term.A(BehaviourAtoms.Ok);
        }

        async ValueTask Restart(Term key, Entry entry)
        {
            long now = Stopwatch.GetTimestamp() / Stopwatch.Frequency;
            double period = (configuration.Period ?? TimeSpan.FromSeconds(BehaviourDefaults.RestartPeriodSeconds)).TotalSeconds;
            while (restarts.Count > 0 && restarts.Peek() < now - period)
                restarts.Dequeue();
            if (restarts.Count >= configuration.Intensity)
                context.Exit(Term.A(ProcessExitReasons.Shutdown));
            restarts.Enqueue(now);
            children.Remove(key);
            var result = (TupleTerm)await entry.Specification.StartResult!(context);
            if (result.Items[0] is Atom { Name: BehaviourAtoms.Error })
            {
                var marker = Restarting(entry.LastPid);
                children[marker] = entry;
                Publish();
                GenServer.Cast(context, context.Self, Term.Tuple(Term.A(SupervisorMessageTags.TryAgainRestart), tag, marker));

                return;
            }
            Install(entry.Specification, result);
        }

        Publish();
        startup.Report(context, Term.Tuple(Term.A(BehaviourAtoms.Ok), context.Self));
        try
        {
            while (true)
            {
                await context.ReduceAsync();
                Term message = (await context.ReceiveAsync(value => value))!;
                if (message is TupleTerm { Items.Count: 3 } call && call.Items[0] is Atom { Name: GenServerMessageTags.Call } && call.Items[1] is TupleTerm { Items.Count: 2 } from && from.Items[0] is Pid caller && from.Items[1] is ReferenceTerm reference)
                {
                    Term result = call.Items[2] switch
                    {
                        Atom { Name: OtpNames.CountChildren } => CountChildren(),
                        Atom { Name: OtpNames.WhichChildren } => Cons.From(children.Keys.Select(ChildTuple)),
                        TupleTerm { Items.Count: 2 } request when request.Items[0] is Atom { Name: OtpNames.StartChild or OtpNames.TerminateChild or OtpNames.RestartChild or OtpNames.DeleteChild or OtpNames.GetChildSpec or OtpNames.WhichChild } operation => await Manage(operation.Name, request.Items[1]),
                        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
                    };
                    GenServer.Reply(context, new(caller, reference), result);
                    continue;
                }
                if (message is TupleTerm { Items.Count: 2 } cast && cast.Items[0] is Atom { Name: GenServerMessageTags.Cast })
                {
                    if (cast.Items[1] is not TupleTerm { Items.Count: 3 } retry || retry.Items[0] is not Atom { Name: SupervisorMessageTags.TryAgainRestart } || !retry.Items[1].Equals(tag))
                        throw new ErlangException(ErlangErrorReasons.FunctionClause);
                    if (retry.Items[2] is not Pid && children.TryGetValue(retry.Items[2], out var entry))
                        await Restart(retry.Items[2], entry);
                    continue;
                }
                if (message is TupleTerm { Items.Count: 3 } system && system.Items[0] is Atom { Name: BehaviourAtoms.System } && system.Items[2] is TupleTerm { Items.Count: 2 } stop && stop.Items[0] is Atom { Name: BehaviourAtoms.Terminate })
                {
                    await StopAll();
                    context.Exit(stop.Items[1]);
                }
                if (message.Equals(Term.A(SupervisorMessageTags.Stop)))
                {
                    await StopAll();
                    context.Exit(Term.A(ProcessExitReasons.Shutdown));
                }
                if (message is not TupleTerm { Items.Count: 3 } exit || exit.Items[0] is not Atom { Name: ProcessMessageTags.LinkedExit } || exit.Items[1] is not Pid pid)
                    continue;
                if (!children.TryGetValue(pid, out var child))
                {
                    if (parent is not null && pid.Equals(parent.Self))
                        context.Exit(exit.Items[2]);
                    continue;
                }
                bool normal = exit.Items[2] is Atom { Name: ProcessExitReasons.Normal or ProcessExitReasons.Shutdown } || exit.Items[2] is TupleTerm { Items.Count: 2 } reason && reason.Items[0] is Atom { Name: ProcessExitReasons.Shutdown };
                if (template.Restart == RestartPolicy.Temporary || template.Restart == RestartPolicy.Transient && normal)
                {
                    children.Remove(pid);
                    Publish();
                    continue;
                }
                await Restart(pid, child);
            }
        }
        finally
        {
            if (context.ExitReason is null)
                await StopAll();
        }
    }
}
