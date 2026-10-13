namespace Erlang.Tests;

internal static class SystemProfileHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var independent = new ProcessRuntime();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = Enumerable.Range(0, SystemProfileHostParameters.Owners).Select(_ => new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        var exchanged = Enumerable.Range(0, SystemProfileHostParameters.Owners).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        var previous = new System.Collections.Concurrent.ConcurrentBag<Term>();
        ProcessHandle[] owners = Enumerable.Range(0, SystemProfileHostParameters.Owners).Select(index => runtime.Spawn(async context =>
        {
            ready[index].SetResult(context);
            await start.Task;
            for (int round = 0; round < SystemProfileHostParameters.Exchanges; round++)
                previous.Add(await Call(context, context.Self, Term.List(Term.A(SystemProfileNames.MonotonicTimestamp))));
            exchanged[index].SetResult();
            await release.Task.WaitAsync(context.Cancellation);

            return Nil.Value;
        })).ToArray();
        ProcessContext[] contexts = await Task.WhenAll(ready.Select(item => item.Task)).WaitAsync(SystemProfileHostParameters.Timeout);
        start.SetResult();
        await Task.WhenAll(exchanged.Select(item => item.Task)).WaitAsync(SystemProfileHostParameters.Timeout);
        Require(previous.Count == SystemProfileHostParameters.Owners * SystemProfileHostParameters.Exchanges);
        Require(previous.Count(value => value.Equals(Term.A(SystemProfileNames.Undefined))) == 1);
        Require(previous.All(value => value.Equals(Term.A(SystemProfileNames.Undefined)) || value is TupleTerm { Items: [Pid pid, Nil] } && owners.Any(owner => owner.Pid.Equals(pid))));
        var settings = (TupleTerm)await Call(contexts[0]);
        Pid receiver = (Pid)settings.Items[0];
        foreach (string effect in new[] { SystemProfileNames.Scheduler, SystemProfileNames.RunnableProcesses, SystemProfileNames.RunnablePorts, SystemProfileNames.Exclusive })
            await Reject(
                contexts[0],
                receiver,
                Term.List(Term.A(effect)),
                SystemProfileNames.NotSupported
            );
        await Reject(
            contexts[0],
            receiver,
            Term.List(Term.A(SystemProfileNames.Scheduler), Term.A(SystemProfileHostParameters.UnknownOption)),
            ErlangErrorReasons.BadArgument
        );
        await Reject(
            contexts[0],
            new Pid(SystemProfileHostParameters.ForeignNode, receiver.Id),
            Term.List(Term.A(SystemProfileNames.Timestamp)),
            ErlangErrorReasons.BadArgument
        );
        Require((await Call(contexts[0])).Equals(settings));
        Require((await Call(contexts[0], Term.A(SystemProfileNames.Undefined), Term.I(42))).Equals(settings));
        Require((await Call(contexts[0], receiver, Term.List(Term.A(SystemProfileNames.Timestamp)))).Equals(Term.A(SystemProfileNames.Undefined)));
        var other = independent.Spawn(async context =>
        {
            Require((await Call(context)).Equals(Term.A(SystemProfileNames.Undefined)));

            return Nil.Value;
        });
        Require((await other.Completion.WaitAsync(SystemProfileHostParameters.Timeout)).Equals(Term.A(ProcessExitReasons.Normal)));
        ProcessContext controller = contexts.First(context => !context.Self.Equals(receiver));
        runtime.Exit(controller, receiver, Term.A(ProcessExitSignals.Kill));
        Require((await Call(controller)).Equals(Term.A(SystemProfileNames.Undefined)));
        release.SetResult();
        await Task.WhenAll(owners.Select(owner => owner.Completion)).WaitAsync(SystemProfileHostParameters.Timeout);
        var portOwner = runtime.Spawn(async context =>
        {
            string executable = Path.GetFullPath(
                Path.Combine(PortHostParameters.RuntimeRoot, OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost),
                System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
            );
            Term port = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                PortNames.Open,
                Term.Tuple(Term.A(PortNames.SpawnExecutable), Term.List(executable.Select(character => Term.I(character)).ToArray())),
                Term.List(Term.Tuple(
                    Term.A(PortNames.Args),
                    Term.List(Term.List(Path.GetFullPath(PortHostParameters.Probe).Select(character => Term.I(character)).ToArray()))
                ))
            );
            Require((await Call(context, port, Term.List(Term.A(SystemProfileNames.Timestamp)))).Equals(Term.A(SystemProfileNames.Undefined)));
            Require((await Call(context)).Equals(Term.Tuple(port, Nil.Value)));
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                PortNames.Close,
                port
            );
            Require((await Call(context)).Equals(Term.A(SystemProfileNames.Undefined)));
            await Reject(
                context,
                port,
                Term.List(Term.A(SystemProfileNames.Timestamp)),
                ErlangErrorReasons.BadArgument
            );

            return Nil.Value;
        });
        Require((await portOwner.Completion.WaitAsync(SystemProfileHostParameters.Timeout)).Equals(Term.A(ProcessExitReasons.Normal)));
    }

    private static ValueTask<Term> Call(ProcessContext context, params Term[] arguments) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        SystemProfileNames.Function,
        arguments
    );

    private static async Task Reject(
        ProcessContext context,
        Term receiver,
        Term options,
        string reason
    )
    {
        try
        {
            await Call(context, receiver, options);
        }
        catch (ErlangException exception)
        {
            Require(exception.Reason.Equals(Term.A(reason)));

            return;
        }

        throw new InvalidOperationException(SystemProfileHostParameters.Failure);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SystemProfileHostParameters.Failure);
    }
}
