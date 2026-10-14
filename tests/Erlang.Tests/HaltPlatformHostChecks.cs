using System.Reflection;

namespace Erlang.Tests;

internal static class HaltPlatformHostChecks
{
    public static async Task Run()
    {
        await TracedPendingReturn();
        await DisplayAndQueuedMonitor();
    }

    private static async Task TracedPendingReturn()
    {
        await using var runtime = new ProcessRuntime();
        var release = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        int continued = 0;
        runtime.Modules.Register(
            HaltPlatformHostNames.Module,
            HaltPlatformHostNames.HeldCall,
            0,
            (context, arguments) =>
        {
            entered.TrySetResult();

            return new ValueTask<Term>(release.Task);
        }
        );
        ProcessHandle target = runtime.Spawn(async context =>
        {
            ready.SetResult(context);
            await context.ReceiveAsync<Term>(value => value);
            await runtime.Modules.Call(context, HaltPlatformHostNames.Module, HaltPlatformHostNames.HeldCall);
            Interlocked.Increment(ref continued);

            return Nil.Value;
        });
        ProcessContext owner = await ready.Task.WaitAsync(HaltPlatformHostNames.Timeout);
        var tracerReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle tracer = runtime.Spawn(async context =>
        {
            tracerReady.SetResult(context);
            await context.ReceiveAsync<Term>(value => value, null);
            await context.ReceiveAsync<Term>(value => value, null);

            return Nil.Value;
        });
        ProcessContext collector = await tracerReady.Task.WaitAsync(HaltPlatformHostNames.Timeout);
        await Call(
            owner,
            HaltPlatformHostNames.Trace,
            target.Pid,
            Term.A(HaltPlatformHostNames.True),
            Term.List(Term.A(HaltPlatformHostNames.Call), Term.Tuple(Term.A(HaltPlatformHostNames.Tracer), tracer.Pid))
        );
        await Call(
            owner,
            HaltPlatformHostNames.Pattern,
            Term.Tuple(Term.A(HaltPlatformHostNames.Module), Term.A(HaltPlatformHostNames.HeldCall), Term.I(0)),
            Term.List(Term.Tuple(Term.A(HaltPlatformHostNames.Wildcard), Nil.Value, Term.List(Term.Tuple(Term.A(HaltPlatformHostNames.ReturnTrace)))))
        );
        foreach (string eventName in new[] { HaltPlatformHostNames.Send, HaltPlatformHostNames.Receive })
            await Call(
                owner,
                HaltPlatformHostNames.Pattern,
                Term.A(eventName),
                Term.List(Term.Tuple(
                    Term.A(HaltPlatformHostNames.Wildcard),
                    Nil.Value,
                    Term.List(Term.Tuple(Term.A(HaltPlatformHostNames.Message), Term.A(HaltPlatformHostNames.True)))
                ))
            );
        runtime.Send(target.Pid, Nil.Value);
        await entered.Task.WaitAsync(HaltPlatformHostNames.Timeout);
        Require((await runtime.RequestImmediateHalt(HaltPlatformHostNames.Status)).Status == HaltPlatformHostNames.Status);
        Require(!runtime.HaltQuiescence.IsCompleted);
        object tracing = Property(runtime, HaltPlatformHostNames.ProcessTracing)!;
        Require(Count(Field(tracing, HaltPlatformHostNames.TracePatterns)!) == 0);
        Require(Count(Field(tracing, HaltPlatformHostNames.TraceSettings)!) == 0);
        Require(Field(tracing, HaltPlatformHostNames.SendPattern) is null && Field(tracing, HaltPlatformHostNames.ReceivePattern) is null);
        Require(Field(tracing, HaltPlatformHostNames.TraceSend) is false && Field(tracing, HaltPlatformHostNames.TraceReceive) is false);
        release.SetResult(Nil.Value);
        await runtime.HaltQuiescence.WaitAsync(HaltPlatformHostNames.Timeout);
        Require(continued == 0);
        Require(owner.Mailbox.Count == 0 && collector.Mailbox.Count == 0);
        Require(!runtime.IsAlive(target.Pid) && !runtime.IsAlive(tracer.Pid));
    }

    private static async Task DisplayAndQueuedMonitor()
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        using var streams = new RuntimeStandardStreams(output, error);
        await using var runtime = new ProcessRuntime(streams);
        var ownerReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requesterReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiverReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? requesterFailure = null;
        ProcessHandle target = runtime.Spawn(async context =>
        {
            using var frame = context.EnterFunction(HaltPlatformHostNames.Module, HaltPlatformHostNames.HeldOwner, 0);
            ownerReady.SetResult(context);
            await hold.Task.WaitAsync(context.Cancellation);

            return Nil.Value;
        });
        ProcessHandle requester = runtime.Spawn(async context =>
        {
            try
            {
                requesterReady.SetResult(context);
                await context.ReceiveAsync<Term>(value => value);
                await Call(
                    context,
                    HaltPlatformHostNames.Display,
                    target.Pid,
                    Term.A(HaltPlatformHostNames.Backtrace)
                );
                throw new InvalidOperationException(HaltPlatformHostNames.Failure);
            }
            catch (Exception exception)
            {
                requesterFailure = exception;
                throw;
            }
        });
        ProcessHandle receiver = runtime.Spawn(async context =>
        {
            receiverReady.SetResult(context);
            await context.ReceiveAsync<Term>(value => value);
            throw new InvalidOperationException(HaltPlatformHostNames.Failure);
        });
        ProcessContext owner = await ownerReady.Task.WaitAsync(HaltPlatformHostNames.Timeout);
        ProcessContext caller = await requesterReady.Task.WaitAsync(HaltPlatformHostNames.Timeout);
        ProcessContext collector = await receiverReady.Task.WaitAsync(HaltPlatformHostNames.Timeout);
        await Call(
            caller,
            HaltPlatformHostNames.SetTracer,
            Term.A(HaltPlatformHostNames.SequentialTracer),
            receiver.Pid
        );
        await Call(
            caller,
            HaltPlatformHostNames.SequentialSet,
            Term.A(HaltPlatformHostNames.Label),
            Term.A(HaltPlatformHostNames.TokenLabel)
        );
        await Call(
            caller,
            HaltPlatformHostNames.TimerStart,
            Term.I(HaltPlatformHostNames.ImmediateTimer),
            requester.Pid,
            Nil.Value
        );
        object runtimeGate = Field(runtime, HaltPlatformHostNames.RuntimeGate)!;
        Require(SpinWait.SpinUntil(
            () =>
        {
            lock (runtimeGate)
                return Count(Property(owner, HaltPlatformHostNames.PendingDisplay)!) == 1;
        },
            HaltPlatformHostNames.Timeout
        ));
        Require(SnapshotCount(owner.Mailbox) == 0);
        Require(Property(Property(caller, HaltPlatformHostNames.SequentialState)!, HaltPlatformHostNames.Token) is not null);
        await Call(
            caller,
            HaltPlatformHostNames.Monitor,
            receiver.Pid,
            Term.List(Term.Tuple(
                Term.A(HaltPlatformHostNames.LongQueue),
                Term.Tuple(Term.I(HaltPlatformHostNames.QueueDisabled), Term.I(HaltPlatformHostNames.QueueEnabled))
            ))
        );
        object monitoring = Property(runtime, HaltPlatformHostNames.SystemMonitoring)!;
        object eventGate = Field(monitoring, HaltPlatformHostNames.EventGate)!;
        Task? dispatch = null;
        Task<RuntimeHaltResult>? selected = null;
        await Task.Run(() =>
        {
            lock (runtimeGate)
            {
                if (!runtime.IsAlive(caller.Self))
                    throw new InvalidOperationException(HaltPlatformHostNames.CallerDiedBeforeElection(caller.Self, requester.Completion.IsCompletedSuccessfully ? requester.Completion.Result : null, requesterFailure));
                runtime.Send(target.Pid, Nil.Value);
                Require(SpinWait.SpinUntil(
                    () =>
                {
                    lock (eventGate)
                        return Count(Field(monitoring, HaltPlatformHostNames.Notifications)!) == 0;
                },
                    HaltPlatformHostNames.Timeout
                ));
                dispatch = (Task)Field(monitoring, HaltPlatformHostNames.Dispatcher)!;
                Require(!dispatch.IsCompleted);
                selected = runtime.RequestImmediateHalt(HaltPlatformHostNames.Status);
                Require(Count(Property(owner, HaltPlatformHostNames.PendingDisplay)!) == 0);
                object? remaining = Property(Property(caller, HaltPlatformHostNames.SequentialState)!, HaltPlatformHostNames.Token);
                if (remaining is not null)
                    throw new InvalidOperationException(HaltPlatformHostNames.TokenResetFailure(
                        caller.Self,
                        remaining,
                        runtime.Lifecycle,
                        runtime.IsAlive(caller.Self)
                    ));
            }
        }).WaitAsync(HaltPlatformHostNames.Timeout);
        Require((await selected!).Status == HaltPlatformHostNames.Status);
        await runtime.HaltQuiescence.WaitAsync(HaltPlatformHostNames.Timeout);
        Require(dispatch!.IsCompletedSuccessfully);
        Require(requesterFailure is ProcessHaltTransfer transfer && ReferenceEquals(transfer.Runtime, runtime));
        lock (eventGate)
            Require(Count(Field(monitoring, HaltPlatformHostNames.Notifications)!) == 0);
        Require(owner.Mailbox.Count == 0 && caller.Mailbox.Count == 0 && collector.Mailbox.Count == 0);
        Require(error.Length == 0);
        Require(!runtime.IsAlive(target.Pid) && !runtime.IsAlive(requester.Pid) && !runtime.IsAlive(receiver.Pid));
    }

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        function,
        arguments
    );

    private static object? Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner);

    private static object? Property(object owner, string name) => owner.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(owner);

    private static int Count(object value) => (int)Property(value, HaltPlatformHostNames.Count)!;

    private static int SnapshotCount(Mailbox mailbox) => ((IReadOnlyList<Term>)mailbox.GetType().GetMethod(HaltPlatformHostNames.Snapshot, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(mailbox, null)!).Count;

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(HaltPlatformHostNames.Failure);
    }
}
