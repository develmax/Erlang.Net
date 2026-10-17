using System.Reflection;

namespace Erlang.Tests;

internal static class RuntimeActorDispatchHostChecks
{
    internal static async Task Run()
    {
        foreach (int capacity in RuntimeActorDispatchHostParameters.Capacities)
            await BoundedSegments(capacity);
        await CapturedContinuationsAndIndependentInitializer();
        await NestedInitializerAfterParentRetirement();
        await CapacityIsolation();
        await CancellationAndExternalDisposal();
        await SelfDisposal();
        await RetirementAndFailedEnqueue();
        await using var defaults = new ProcessRuntime();
        Require(defaults.ActorDispatchCapacity == Environment.ProcessorCount);
        await defaults.DisposeAsync();
        await Completion(defaults).WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        foreach (int invalid in new[] { 0, -1 })
        {
            try
            {
                _ = new ProcessRuntime(invalid);
                Require(false);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
        Console.WriteLine(RuntimeActorDispatchHostNames.Status);
    }

    private static async Task BoundedSegments(int capacity)
    {
        await using var runtime = new ProcessRuntime(capacity);
        using var release = new ManualResetEventSlim();
        var firstWave = NewSignal();
        var actors = new List<ProcessHandle>();
        int active = 0, maximum = 0, started = 0;
        try
        {
            for (int index = 0; index < capacity + RuntimeActorDispatchHostParameters.ExtraQueuedActors; index++)
            {
                actors.Add(runtime.Spawn(context =>
                {
                    Require(TaskScheduler.Current.MaximumConcurrencyLevel == RuntimeActorDispatchHostParameters.PerProcessConcurrency);
                    int now = Interlocked.Increment(ref active);
                    RecordMaximum(ref maximum, now);
                    Interlocked.Increment(ref started);
                    if (now == capacity)
                        firstWave.TrySetResult();
                    try
                    {
                        Require(release.Wait(RuntimeActorDispatchHostParameters.Deadline, context.Cancellation));
                    }
                    finally
                    {
                        Interlocked.Decrement(ref active);
                    }

                    return ValueTask.FromResult<Term>(Nil.Value);
                }));
            }
            await firstWave.Task.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
            Require(Volatile.Read(ref started) == capacity);
            Require(Volatile.Read(ref maximum) == capacity);
        }
        finally
        {
            release.Set();
        }
        foreach (var actor in actors)
            Require((await actor.Completion.WaitAsync(RuntimeActorDispatchHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        Require(maximum <= capacity);
        await runtime.DisposeAsync();
        await Completion(runtime).WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
    }

    private static async Task CapturedContinuationsAndIndependentInitializer()
    {
        await using var runtime = new ProcessRuntime(1);
        var waiting = NewSignal();
        var send = NewSignal();
        var actor = runtime.Spawn(async context =>
        {
            TaskScheduler scheduler = TaskScheduler.Current;
            Require(!ReferenceEquals(scheduler, TaskScheduler.Default));
            Require((await context.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeActorDispatchHostNames.SystemInfo,
                Term.A(RuntimeActorDispatchHostNames.Schedulers)
            )).Equals(Term.I(runtime.ActorDispatchCapacity)));
            await Task.Yield();
            Require(ReferenceEquals(TaskScheduler.Current, scheduler));
            var child = runtime.Spawn(async childContext =>
            {
                TaskScheduler childScheduler = TaskScheduler.Current;
                Require(!ReferenceEquals(childScheduler, scheduler));
                Require(!ReferenceEquals(childScheduler, TaskScheduler.Default));
                Require(childScheduler.MaximumConcurrencyLevel == RuntimeActorDispatchHostParameters.PerProcessConcurrency);
                await send.Task;
                Require(ReferenceEquals(TaskScheduler.Current, childScheduler));
                runtime.Send(context.Self, Term.A(RuntimeActorDispatchHostNames.Message));

                return Nil.Value;
            });
            ValueTask<Term?> receive = context.ReceiveAsync<Term>(message => message,
                RuntimeActorDispatchHostParameters.Deadline);
            Require(!receive.IsCompleted);
            waiting.TrySetResult();
            Require((await receive)!.Equals(Term.A(RuntimeActorDispatchHostNames.Message)));
            Require(ReferenceEquals(TaskScheduler.Current, scheduler));
            Require((await child.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
            await runtime.InitializeModulesAsync(registry =>
            {
                Require(ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default));
                registry.Register(
                    RuntimeActorDispatchHostNames.Module,
                    RuntimeActorDispatchHostNames.Function,
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Term.A(RuntimeActorDispatchHostNames.InitializedValue))
                );
            });
            Require(ReferenceEquals(TaskScheduler.Current, scheduler));
            Require((await runtime.Modules.Call(context, RuntimeActorDispatchHostNames.Module,
                RuntimeActorDispatchHostNames.Function)).Equals(Term.A(RuntimeActorDispatchHostNames.InitializedValue)));

            return Nil.Value;
        });
        await waiting.Task.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        send.TrySetResult();
        Require((await actor.Completion.WaitAsync(RuntimeActorDispatchHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        await runtime.DisposeAsync();
        await Completion(runtime).WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
    }

    private static async Task NestedInitializerAfterParentRetirement()
    {
        await using var parent = new ProcessRuntime(1);
        ProcessRuntime? child = null;
        var created = NewSignal();
        var finish = NewSignal();
        bool initialDefaultScheduler = false;
        var actor = parent.Spawn(async context =>
        {
            Require(!ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default));
            child = new ProcessRuntime(1);
            await child.InitializeModulesAsync(registry =>
            {
                initialDefaultScheduler = ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default);
                registry.Register(
                    RuntimeActorDispatchHostNames.Module,
                    RuntimeActorDispatchHostNames.Function,
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Term.A(RuntimeActorDispatchHostNames.InitializedValue))
                );
            });
            created.TrySetResult();
            await finish.Task;

            return Nil.Value;
        });
        try
        {
            await created.Task.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
            object dispatcher = typeof(ProcessRuntime).GetField(RuntimeActorDispatchHostNames.DispatcherField,
                BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(parent)!;
            Task retiring = (Task)dispatcher.GetType().GetMethod(RuntimeActorDispatchHostNames.RetireMethod)!.Invoke(dispatcher, null)!;
            Require(!retiring.IsCompleted);
            finish.TrySetResult();
            Require((await actor.Completion.WaitAsync(RuntimeActorDispatchHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
            await retiring.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
            Require(retiring.IsCompletedSuccessfully);
            Require(initialDefaultScheduler);
            Require(child is not null);
            await child!.InitializeModulesAsync(registry =>
            {
                Require(ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default));
                registry.Register(
                    RuntimeActorDispatchHostNames.PostRetirementModule,
                    RuntimeActorDispatchHostNames.Function,
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Term.A(RuntimeActorDispatchHostNames.InitializedValue))
                );
            }).AsTask().WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
            var childActor = child.Spawn(async context =>
            {
                Require(!ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default));
                Require((await child.Modules.Call(context, RuntimeActorDispatchHostNames.PostRetirementModule,
                    RuntimeActorDispatchHostNames.Function)).Equals(Term.A(RuntimeActorDispatchHostNames.InitializedValue)));

                return Nil.Value;
            });
            Require((await childActor.Completion.WaitAsync(RuntimeActorDispatchHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        }
        finally
        {
            finish.TrySetResult();
            if (child is not null)
            {
                await child.DisposeAsync().AsTask().WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
                await Completion(child).WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
            }
        }
    }

    private static async Task CapacityIsolation()
    {
        await using var first = new ProcessRuntime(1);
        await using var second = new ProcessRuntime(2);
        using var release = new ManualResetEventSlim();
        var entered = NewSignal();
        var held = first.Spawn(context =>
        {
            entered.TrySetResult();
            Require(release.Wait(RuntimeActorDispatchHostParameters.Deadline, context.Cancellation));

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        try
        {
            await entered.Task.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
            var independent = second.Spawn(context =>
            {
                Require(TaskScheduler.Current.MaximumConcurrencyLevel == RuntimeActorDispatchHostParameters.PerProcessConcurrency);

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            Require((await independent.Completion.WaitAsync(RuntimeActorDispatchHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
            Require(first.ActorDispatchCapacity == 1 && second.ActorDispatchCapacity == 2);
        }
        finally
        {
            release.Set();
        }
        Require((await held.Completion.WaitAsync(RuntimeActorDispatchHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        await first.DisposeAsync();
        await second.DisposeAsync();
        await Task.WhenAll(Completion(first), Completion(second)).WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
    }

    private static async Task CancellationAndExternalDisposal()
    {
        await using var runtime = new ProcessRuntime(1);
        var entered = NewSignal();
        var waiting = runtime.Spawn(async context =>
        {
            ValueTask<Term?> receive = context.ReceiveAsync<Term>(message => message);
            Require(!receive.IsCompleted);
            entered.TrySetResult();
            _ = await receive;

            return Nil.Value;
        });
        await entered.Task.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        var killer = runtime.Spawn(context =>
        {
            runtime.Exit(context, waiting.Pid, Term.A(ProcessExitSignals.Kill));

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        Require((await waiting.Completion.WaitAsync(RuntimeActorDispatchHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Killed)));
        _ = await killer.Completion.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        var shutdownEntered = NewSignal();
        var shutdownWaiter = runtime.Spawn(async context =>
        {
            ValueTask<Term?> receive = context.ReceiveAsync<Term>(message => message);
            Require(!receive.IsCompleted);
            shutdownEntered.TrySetResult();
            _ = await receive;

            return Nil.Value;
        });
        await shutdownEntered.Task.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        await runtime.DisposeAsync();
        Require((await shutdownWaiter.Completion).Equals(Term.A(ProcessExitReasons.Shutdown)));
        await Completion(runtime).WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        Require(Completion(runtime).IsCompletedSuccessfully);
        try
        {
            _ = runtime.Spawn(_ => ValueTask.FromResult<Term>(Nil.Value));
            Require(false);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static async Task SelfDisposal()
    {
        await using var runtime = new ProcessRuntime(1);
        var returned = NewSignal();
        var actor = runtime.Spawn(async context =>
        {
            await runtime.DisposeAsync();
            returned.TrySetResult();

            return Nil.Value;
        });
        await returned.Task.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        await Completion(runtime).WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        Require((await actor.Completion).Equals(Term.A(ProcessExitReasons.Shutdown)));
    }

    private static async Task RetirementAndFailedEnqueue()
    {
        await using var runtime = new ProcessRuntime(
            ProcessNodeDefaults.Local,
            null,
            RuntimeActorDispatchHostParameters.RollbackProcessLimit,
            null,
            1
        );
        var entered = NewSignal();
        var finish = NewSignal();
        var actor = runtime.Spawn(async context =>
        {
            entered.TrySetResult();
            await finish.Task;
            await Task.Yield();

            return Nil.Value;
        });
        await entered.Task.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        object dispatcher = typeof(ProcessRuntime).GetField(RuntimeActorDispatchHostNames.DispatcherField,
            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(runtime)!;
        Task retiring = (Task)dispatcher.GetType().GetMethod(RuntimeActorDispatchHostNames.RetireMethod)!.Invoke(dispatcher, null)!;
        Require(!retiring.IsCompleted);
        for (int index = 0; index < RuntimeActorDispatchHostParameters.RollbackAttempts; index++)
        {
            try
            {
                _ = runtime.Spawn(_ => ValueTask.FromResult<Term>(Nil.Value));
                Require(false);
            }
            catch (ObjectDisposedException)
            {
            }
        }
        finish.TrySetResult();
        Require((await actor.Completion.WaitAsync(RuntimeActorDispatchHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        await retiring.WaitAsync(RuntimeActorDispatchHostParameters.Deadline);
        await runtime.DisposeAsync();
    }

    private static Task Completion(ProcessRuntime runtime)
        => (Task)typeof(ProcessRuntime).GetProperty(RuntimeActorDispatchHostNames.CompletionProperty,
            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(runtime)!;

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void RecordMaximum(ref int maximum, int value)
    {
        int previous;
        do
        {
            previous = Volatile.Read(ref maximum);
            if (previous >= value)
                return;
        }
        while (Interlocked.CompareExchange(ref maximum, value, previous) != previous);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(RuntimeActorDispatchHostNames.Failure);
    }
}
