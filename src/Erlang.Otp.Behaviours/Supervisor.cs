using System.Diagnostics;

namespace Erlang.Otp;

public static class Supervisor
{
    private sealed record Child(ChildSpec Spec, ProcessHandle Process);

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
            IReadOnlyList<ChildSpec> specifications = [];
            var children = Array.Empty<Child?>();
            RestartStrategy strategy = RestartStrategy.OneForOne;
            int intensity = BehaviourDefaults.RestartIntensity;
            TimeSpan? period = null;
            var restarts = new Queue<long>();
            var retired = new HashSet<Pid>();

            void Publish() => handle.Publish(children.OfType<Child>().Select(c => (c.Spec.Id, c.Process.Pid)));

            async ValueTask StartChild(int index)
            {
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
                children[index] = new(specifications[index], process);
                Publish();
            }

            async ValueTask StopChild(int index)
            {
                if (children[index] is not { } child)
                    return;
                retired.Add(child.Process.Pid);
                children[index] = null;
                Publish();
                runtime.Exit(context, child.Process.Pid, Term.A(child.Spec.BrutalKill ? ProcessExitSignals.Kill : ProcessExitReasons.Shutdown));
                try
                {
                    if (child.Spec.InfiniteShutdown)
                        await child.Process.Completion;
                    else
                        await child.Process.Completion.WaitAsync(child.Spec.Shutdown ?? TimeSpan.FromMilliseconds(BehaviourDefaults.ChildShutdownMilliseconds));
                }
                catch (TimeoutException)
                {
                    runtime.Exit(context, child.Process.Pid, Term.A(ProcessExitSignals.Kill));
                    await child.Process.Completion;
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
                specifications = configuration.Children;
                strategy = configuration.Strategy;
                intensity = configuration.Intensity;
                period = configuration.Period;
                if (intensity < 0 || period <= TimeSpan.Zero || specifications.Select(s => s.Id).Distinct().Count() != specifications.Count)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                handle.Specifications = specifications;
                children = new Child?[specifications.Count];
                for (int i = 0; i < children.Length; i++)
                    await StartChild(i);
                if (parent is not null)
                    runtime.Link(context, parent.Self);
                ready.TrySetResult();
                while (true)
                {
                    var message = (await context.ReceiveAsync(t => t))!;
                    if (message is TupleTerm { Items.Count: 3 } call && call.Items[0] is Atom { Name: GenServerMessageTags.Call } && call.Items[1] is TupleTerm { Items.Count: 2 } from && from.Items[0] is Pid caller && from.Items[1] is ReferenceTerm reference)
                    {
                        Term reply = call.Items[2] switch
                        {
                            Atom { Name: OtpNames.WhichChildren } => OtpModules.WhichChildren(handle),
                            Atom { Name: OtpNames.CountChildren } => OtpModules.CountChildren(handle),
                            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
                        };
                        GenServer.Reply(context, new(caller, reference), reply);
                        continue;
                    }
                    if (message is TupleTerm { Items.Count: 3 } system && system.Items[0] is Atom { Name: BehaviourAtoms.System } && system.Items[2] is TupleTerm { Items.Count: 2 } stop && stop.Items[0] is Atom { Name: BehaviourAtoms.Terminate })
                    {
                        for (int i = children.Length - 1; i >= 0; i--)
                            await StopChild(i);
                        context.Exit(stop.Items[1]);
                    }
                    if (message.Equals(Term.A(SupervisorMessageTags.Stop)))
                    {
                        for (int i = children.Length - 1; i >= 0; i--)
                            await StopChild(i);
                        context.Exit(Term.A(ProcessExitReasons.Shutdown));
                    }
                    if (message is not TupleTerm { Items.Count: 3 } exit || !exit.Items[0].Equals(Term.A(ProcessMessageTags.LinkedExit)) || exit.Items[1] is not Pid pid)
                        continue;
                    if (retired.Remove(pid))
                        continue;
                    int failed = Array.FindIndex(children, c => c is not null && c.Process.Pid.Equals(pid));
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
                    if (spec.Restart == RestartPolicy.Temporary || spec.Restart == RestartPolicy.Transient && normal)
                        continue;
                    long now = Stopwatch.GetTimestamp();
                    while (restarts.Count > 0 && Stopwatch.GetElapsedTime(restarts.Peek(), now) > (period ?? TimeSpan.FromSeconds(BehaviourDefaults.RestartPeriodSeconds)))
                        restarts.Dequeue();
                    restarts.Enqueue(now);
                    if (restarts.Count > intensity)
                        context.Exit(Term.A(ProcessExitReasons.Shutdown));
                    int first = strategy == RestartStrategy.OneForAll ? 0 : failed;
                    int last = strategy == RestartStrategy.OneForOne ? failed : children.Length - 1;
                    for (int i = last; i >= first; i--)
                        if (i != failed)
                            await StopChild(i);
                    for (int i = first; i <= last; i++)
                        if (specifications[i].Restart != RestartPolicy.Temporary)
                            await StartChild(i);
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
                    for (int i = children.Length - 1; i >= 0; i--)
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
