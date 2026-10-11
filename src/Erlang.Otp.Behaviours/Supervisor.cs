using System.Diagnostics;

namespace Erlang.Otp;

public static class Supervisor
{
    private sealed record Child(ChildSpec Spec, Pid Pid);

    public static ValueTask<SupervisorHandle> Start(
        ProcessRuntime runtime,
        IReadOnlyList<ChildSpec> specifications,
        RestartStrategy strategy = RestartStrategy.OneForOne,
        int intensity = 1,
        TimeSpan? period = null,
        ProcessContext? parent = null
    )
        => StartCore(runtime, _ => ValueTask.FromResult(new SupervisorConfiguration(
            specifications,
            strategy,
            intensity,
            period
        )), parent);

    private static async ValueTask<SupervisorHandle> StartCore(
        ProcessRuntime runtime,
        Func<ProcessContext, ValueTask<SupervisorConfiguration>> initialize,
        ProcessContext? parent,
        string? name = null
    )
    {

        var handle = new SupervisorHandle();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        handle.Process = runtime.Spawn(
            async context =>
        {
            context.TrapExits = true;
            var specifications = new List<ChildSpec>();
            var children = new List<Child?>();
            RestartStrategy strategy = RestartStrategy.OneForOne;
            int intensity = BehaviourDefaults.RestartIntensity;
            TimeSpan? period = null;
            AutoShutdownPolicy automatic = AutoShutdownPolicy.Never;
            var restarts = new Queue<long>();
            var restarting = new HashSet<Term>();
            Term retryTag = await runtime.Modules.Call(context, ErlangModuleNames.Module, ErlangModuleNames.MakeReference);

            void Publish()
            {
                handle.Specifications = Array.AsReadOnly(specifications.ToArray());
                handle.Publish(children.OfType<Child>().Select(c => (c.Spec.Id, c.Pid)));
                handle.PublishRestarting(restarting);
            }

            void RemoveChild(int index)
            {
                restarting.Remove(specifications[index].Id);
                specifications.RemoveAt(index);
                children.RemoveAt(index);
                Publish();
            }

            void RemoveInactiveTemporary()
            {
                for (int i = children.Count - 1; i >= 0; i--)
                    if (children[i] is null && specifications[i].Restart == RestartPolicy.Temporary)
                        RemoveChild(i);
            }

            bool ShouldAutoShutdown(ChildSpec child)
            {
                if (!child.Significant || automatic == AutoShutdownPolicy.Never)
                    return false;
                if (automatic == AutoShutdownPolicy.AnySignificant)
                    return true;

                return !specifications.Where((specification, index) => specification.Significant && (children[index] is not null || restarting.Contains(specification.Id))).Any();
            }

            async ValueTask<Term> InvokeStart(ChildSpec spec)
            {
                if (spec.StartResult is { } moduleStart)
                    return await moduleStart(context);
                try
                {
                    var process = spec.OptionalStart is { } optional ? await optional(context) : await spec.Start(context);

                    return Term.Tuple(Term.A(BehaviourAtoms.Ok), process?.Pid ?? (Term)Term.A(SupervisorAtoms.Undefined));
                }
                catch (ErlangException exception)
                {
                    return Term.Tuple(Term.A(BehaviourAtoms.Error), exception.Reason);
                }
            }

            void InstallChild(int index, TupleTerm result)
            {
                restarting.Remove(specifications[index].Id);
                if (result.Items[1] is Pid pid)
                {
                    if (specifications[index].StartResult is null)
                    {
                        runtime.GetProcessHandle(pid);
                        runtime.Link(context, pid);
                    }
                    children[index] = new(specifications[index], pid);
                }
                Publish();
            }

            Term Error(string reason) => Term.Tuple(Term.A(BehaviourAtoms.Error), Term.A(reason));

            async ValueTask<Term> ManageChild(string operation, Term argument)
            {
                if (operation == OtpNames.StartChild)
                {
                    ChildSpec spec;
                    try
                    {
                        spec = ErlangSupervisor.ParseChild(argument, automatic);
                    }
                    catch (ErlangException exception)
                    {
                        return Term.Tuple(Term.A(BehaviourAtoms.Error), exception.Reason);
                    }
                    int existing = specifications.FindIndex(s => s.Id.Equals(spec.Id));
                    if (existing >= 0)
                        return children[existing] is { } running
                            ? Term.Tuple(Term.A(BehaviourAtoms.Error), Term.Tuple(Term.A(SupervisorAtoms.AlreadyStarted), running.Pid))
                            : Error(SupervisorAtoms.AlreadyPresent);
                    var result = (TupleTerm)await InvokeStart(spec);
                    if (result.Items[0] is Atom { Name: BehaviourAtoms.Error })
                    {
                        Term reason = result.Items[1];
                        if (reason is TupleTerm { Items.Count: 2 } already && already.Items[0] is Atom { Name: SupervisorAtoms.AlreadyStarted })
                            return result;

                        return Term.Tuple(Term.A(BehaviourAtoms.Error), Term.Tuple(reason, ErlangSupervisor.ChildRecord(spec)));
                    }
                    if (result.Items[1] is Pid || spec.Restart != RestartPolicy.Temporary)
                    {
                        if (spec.Restart == RestartPolicy.Temporary && spec.StartMfa is TupleTerm start)
                            spec = spec with { StartMfa = Term.Tuple(start.Items[0], start.Items[1], Term.A(SupervisorAtoms.Undefined)) };
                        specifications.Add(spec);
                        children.Add(null);
                        InstallChild(children.Count - 1, result);
                    }

                    return result;
                }
                int index = specifications.FindIndex(s => s.Id.Equals(argument));
                if (index < 0)
                    return Error(SupervisorAtoms.NotFound);
                if (operation == OtpNames.GetChildSpec)
                    return Term.Tuple(Term.A(BehaviourAtoms.Ok), ErlangSupervisor.ChildSpecification(specifications[index]));
                if (operation == OtpNames.WhichChild)
                    return Term.Tuple(
                        Term.A(BehaviourAtoms.Ok),
                        Term.Tuple(
                            specifications[index].Id,
                            restarting.Contains(argument) ? Term.A(SupervisorAtoms.Restarting) : children[index]?.Pid ?? (Term)Term.A(SupervisorAtoms.Undefined),
                            specifications[index].Type ?? Term.A(SupervisorAtoms.Worker),
                            specifications[index].Modules ?? Nil.Value
                        )
                    );
                if (operation == OtpNames.TerminateChild)
                {
                    await StopChild(index);
                    if (specifications[index].Restart == RestartPolicy.Temporary)
                        RemoveChild(index);

                    return Term.A(BehaviourAtoms.Ok);
                }
                if (restarting.Contains(argument))
                    return Error(SupervisorAtoms.Restarting);
                if (children[index] is not null)
                    return Error(SupervisorAtoms.Running);
                if (operation == OtpNames.DeleteChild)
                {
                    RemoveChild(index);

                    return Term.A(BehaviourAtoms.Ok);
                }
                var restarted = (TupleTerm)await InvokeStart(specifications[index]);
                if (restarted.Items[0] is Atom { Name: BehaviourAtoms.Ok })
                    InstallChild(index, restarted);

                return restarted;
            }

            async ValueTask StartChild(int index)
            {
                if (specifications[index].StartResult is not null)
                {
                    var result = (TupleTerm)await InvokeStart(specifications[index]);
                    if (result.Items[0] is Atom { Name: BehaviourAtoms.Error })
                        throw new ErlangException(
                            Term.Tuple(
                                Term.A(ProcessExitReasons.Shutdown),
                                Term.Tuple(Term.A(SupervisorErrorReasons.FailedToStartChild), specifications[index].Id, result.Items[1])
                            ),
                            ErlangExceptionClasses.Exit
                        );
                    InstallChild(index, result);

                    return;
                }
                ProcessHandle? process;
                try
                {
                    process = specifications[index].OptionalStart is { } optional ? await optional(context) : await specifications[index].Start(context);
                }
                catch (ErlangException exception)
                {
                    throw new ErlangException(
                        Term.Tuple(
                            Term.A(ProcessExitReasons.Shutdown),
                            Term.Tuple(Term.A(SupervisorErrorReasons.FailedToStartChild), specifications[index].Id, exception.Reason)
                        ),
                        ErlangExceptionClasses.Exit
                    );
                }
                if (process is null)
                    return;
                if (process.Completion.IsCompleted)
                    throw new ErlangException(Term.Tuple(Term.A(SupervisorErrorReasons.FailedToStartChild), specifications[index].Id));
                runtime.Link(context, process.Pid);
                children[index] = new(specifications[index], process.Pid);
                Publish();
            }

            async ValueTask StopChild(int index)
            {
                restarting.Remove(specifications[index].Id);
                if (children[index] is not { } child)
                {
                    Publish();

                    return;
                }
                children[index] = null;
                Publish();
                var monitor = runtime.Monitor(context, child.Pid);

                TupleTerm? Down(Term message) => message is TupleTerm { Items.Count: 5 } down && down.Items[0] is Atom { Name: ProcessMessageTags.MonitorDown } && down.Items[1].Equals(monitor) && down.Items[2] is Atom { Name: ProcessMonitorKinds.Process } && down.Items[3].Equals(child.Pid) ? down : null;
                try
                {
                    runtime.Exit(context, child.Pid, Term.A(child.Spec.BrutalKill ? ProcessExitSignals.Kill : ProcessExitReasons.Shutdown));
                    TimeSpan? timeout = child.Spec.BrutalKill || child.Spec.InfiniteShutdown ? null : child.Spec.Shutdown ?? TimeSpan.FromMilliseconds(BehaviourDefaults.ChildShutdownMilliseconds);
                    if (await context.ReceiveAsync(Down, timeout) is null)
                    {
                        runtime.Exit(context, child.Pid, Term.A(ProcessExitSignals.Kill));
                        await context.ReceiveAsync(Down);
                    }
                }
                finally
                {
                    runtime.Demonitor(context, monitor, true);
                    runtime.Unlink(context, child.Pid);
                    context.Mailbox.Remove(message => message is TupleTerm { Items.Count: 3 } exit && exit.Items[0] is Atom { Name: ProcessMessageTags.LinkedExit } && exit.Items[1].Equals(child.Pid));
                }
            }

            async ValueTask RestartAutomatically(int failed)
            {
                long now = Stopwatch.GetTimestamp() / Stopwatch.Frequency;
                double window = (period ?? TimeSpan.FromSeconds(BehaviourDefaults.RestartPeriodSeconds)).TotalSeconds;
                while (restarts.Count > 0 && restarts.Peek() < now - window)
                    restarts.Dequeue();
                if (restarts.Count >= intensity)
                    context.Exit(Term.A(ProcessExitReasons.Shutdown));
                restarts.Enqueue(now);
                int first = strategy == RestartStrategy.OneForAll ? 0 : failed;
                int last = strategy == RestartStrategy.OneForOne ? failed : children.Count - 1;
                var group = specifications.Skip(first).Take(last - first + 1).Select(s => s.Id).ToArray();
                if (strategy != RestartStrategy.OneForOne)
                {
                    foreach (Term id in group.Reverse())
                        await StopChild(specifications.FindIndex(s => s.Id.Equals(id)));
                    RemoveInactiveTemporary();
                }
                foreach (Term id in group)
                {
                    int index = specifications.FindIndex(s => s.Id.Equals(id));
                    if (index < 0)
                        continue;
                    var result = (TupleTerm)await InvokeStart(specifications[index]);
                    if (result.Items[0] is Atom { Name: BehaviourAtoms.Error })
                    {
                        restarting.Add(id);
                        Publish();
                        GenServer.Cast(context, context.Self, Term.Tuple(Term.A(SupervisorMessageTags.TryAgainRestart), retryTag, id));

                        return;
                    }
                    InstallChild(index, result);
                }
            }
            try
            {
                if (name is not null)
                {
                    if (runtime.WhereIs(name) is Pid existing)
                        throw new ErlangException(Term.Tuple(Term.A(BehaviourAtoms.AlreadyStarted), existing), ErlangExceptionClasses.Exit);
                    runtime.Register(name, context.Self);
                }
                var configuration = await initialize(context);
                if (configuration.Ignore)
                {
                    ready.TrySetException(new ErlangException(Term.A(BehaviourAtoms.Ignore), ErlangExceptionClasses.Exit));

                    return Term.A(BehaviourAtoms.Ok);
                }
                if (configuration.Strategy == RestartStrategy.SimpleOneForOne)
                {
                    if (configuration.Children.Count != BehaviourDefaults.DynamicTemplateSpecifications || configuration.Children[0].StartMfa is null)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);

                    return await SimpleSupervisor.Run(
                        context,
                        configuration,
                        parent,
                        ready,
                        handle
                    );
                }
                specifications.AddRange(configuration.Children);
                strategy = configuration.Strategy;
                intensity = configuration.Intensity;
                period = configuration.Period;
                automatic = configuration.AutoShutdown;
                if (intensity < 0 || period <= TimeSpan.Zero || specifications.Select(s => s.Id).Distinct().Count() != specifications.Count)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                children.AddRange(new Child?[specifications.Count]);
                for (int i = 0; i < children.Count; i++)
                    await StartChild(i);
                RemoveInactiveTemporary();
                Publish();
                if (parent is not null)
                    runtime.Link(context, parent.Self);
                ready.TrySetResult();
                while (true)
                {
                    await context.ReduceAsync();
                    var message = (await context.ReceiveAsync(t => t))!;
                    if (message is TupleTerm { Items.Count: 3 } call && call.Items[0] is Atom { Name: GenServerMessageTags.Call } && call.Items[1] is TupleTerm { Items.Count: 2 } from && from.Items[0] is Pid caller && from.Items[1] is ReferenceTerm reference)
                    {
                        Term reply = call.Items[2] switch
                        {
                            Atom { Name: OtpNames.WhichChildren } => OtpModules.WhichChildren(handle),
                            Atom { Name: OtpNames.CountChildren } => OtpModules.CountChildren(handle, runtime),
                            TupleTerm { Items.Count: 2 } request when request.Items[0] is Atom { Name: OtpNames.StartChild or OtpNames.TerminateChild or OtpNames.RestartChild or OtpNames.DeleteChild or OtpNames.GetChildSpec or OtpNames.WhichChild } operation => await ManageChild(operation.Name, request.Items[1]),
                            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
                        };
                        GenServer.Reply(context, new(caller, reference), reply);
                        continue;
                    }
                    if (message is TupleTerm { Items.Count: 2 } cast && cast.Items[0] is Atom { Name: GenServerMessageTags.Cast })
                    {
                        if (cast.Items[1] is not TupleTerm { Items.Count: 3 } retry || retry.Items[0] is not Atom { Name: SupervisorMessageTags.TryAgainRestart } || !retry.Items[1].Equals(retryTag))
                            throw new ErlangException(ErlangErrorReasons.FunctionClause);
                        if (restarting.Contains(retry.Items[2]))
                        {
                            int index = specifications.FindIndex(s => s.Id.Equals(retry.Items[2]));
                            if (index >= 0)
                                await RestartAutomatically(index);
                        }
                        continue;
                    }
                    if (message is TupleTerm { Items.Count: 3 } system && system.Items[0] is Atom { Name: BehaviourAtoms.System } && system.Items[2] is TupleTerm { Items.Count: 2 } stop && stop.Items[0] is Atom { Name: BehaviourAtoms.Terminate })
                    {
                        for (int i = children.Count - 1; i >= 0; i--)
                            await StopChild(i);
                        context.Exit(stop.Items[1]);
                    }
                    if (message.Equals(Term.A(SupervisorMessageTags.Stop)))
                    {
                        for (int i = children.Count - 1; i >= 0; i--)
                            await StopChild(i);
                        context.Exit(Term.A(ProcessExitReasons.Shutdown));
                    }
                    if (message is not TupleTerm { Items.Count: 3 } exit || !exit.Items[0].Equals(Term.A(ProcessMessageTags.LinkedExit)) || exit.Items[1] is not Pid pid)
                        continue;
                    int failed = children.FindIndex(c => c is not null && c.Pid.Equals(pid));
                    if (failed < 0)
                    {
                        if (parent is not null && pid.Equals(parent.Self))
                            context.Exit(exit.Items[2]);
                        continue;
                    }
                    var spec = children[failed]!.Spec;
                    children[failed] = null;
                    Publish();
                    bool normal = exit.Items[2].Equals(Term.A(ProcessExitReasons.Normal)) || exit.Items[2].Equals(Term.A(ProcessExitReasons.Shutdown)) || exit.Items[2] is TupleTerm { Items.Count: 2 } reason && reason.Items[0].Equals(Term.A(ProcessExitReasons.Shutdown));
                    if (spec.Restart == RestartPolicy.Temporary)
                    {
                        RemoveChild(failed);
                    }
                    if (spec.Restart == RestartPolicy.Temporary || spec.Restart == RestartPolicy.Transient && normal)
                    {
                        if (ShouldAutoShutdown(spec))
                            context.Exit(Term.A(ProcessExitReasons.Shutdown));
                        continue;
                    }
                    await RestartAutomatically(failed);
                }
            }
            catch (Exception ex)
            {
                ready.TrySetException(ex);
                throw;
            }
            finally
            {
                if (context.ExitReason is null)
                    for (int i = children.Count - 1; i >= 0; i--)
                        await StopChild(i);
                Publish();
            }
        },
            null,
            false
        );
        try
        {
            await ready.Task;
        }
        catch
        {
            await handle.Process.Completion;
            throw;
        }

        return handle;
    }

    internal static ValueTask<SupervisorHandle> StartModule(
        ProcessRuntime runtime,
        string module,
        Term arguments,
        ProcessContext parent,
        string? name = null
    ) => StartCore(
        runtime,
        c => ErlangSupervisor.Initialize(c, module, arguments),
        parent,
        name
    );

    public static async ValueTask Stop(ProcessRuntime runtime, SupervisorHandle supervisor)
    {
        runtime.Send(supervisor.Process.Pid, Term.A(SupervisorMessageTags.Stop));
        await supervisor.Process.Completion;
    }
}
