using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;
using static Erlang.Tests.AtomicsLifecycle586Names;

namespace Erlang.Tests;

internal static class AtomicsLifecycle586Checks
{
    internal static async Task ShutdownWaitsOutsideRuntimeGate()
    {
        foreach (bool halt in new[] { false, true })
            foreach (bool invocation in new[] { true, false })
                await Run(halt, invocation, false);
    }

    internal static Task BackingSurvivesOwnedHeapRetirement() => Run(false, true, true);

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }

    private static object Invoke(object target, string method, params object[] arguments)
        => target.GetType().GetMethod(method, Members)!.Invoke(target, arguments)!;

    private static IDisposable Hold(ProcessRuntime runtime, Term reference, bool invocation)
    {
        if (invocation)
        {
            object owner = typeof(ProcessRuntime).GetField(Owner, Members)!.GetValue(runtime)!;

            return (IDisposable)Invoke(owner, Enter);
        }
        object entry = typeof(ResourceReferenceTerm).GetProperty(AtomicsHost413Names.Entry, Members)!.GetValue(reference)!;
        Type type = typeof(ProcessRuntime).Assembly.GetType(AtomicsHost413Names.RootType)!;
        object root = entry.GetType().GetMethod(AtomicsHost413Names.Read, Members)!.MakeGenericMethod(type).Invoke(entry, null)!;
        object array = type.GetProperty(AtomicsHost413Names.Array, Members)!.GetValue(root)!;

        return (IDisposable)Invoke(array, AtomicsHost413Names.Acquire);
    }

    private static async Task Run(bool halt, bool invocation, bool owned)
    {
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? held = null;
        IDisposable? frame = null;
        Task? shutdown = null;
        var failures = new List<Exception>();
        try
        {
            runtime = owned ? OwnedGuest506RuntimeFactory.Create() : await AtomicsHost413Access.Create();
            if (owned)
                await runtime.InitializeModulesAsync(registry => RuntimeBootstrapAtomics.Register(runtime, registry));
            actor = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext context = await actor.Ready.WaitAsync(Deadline);
            ProcessRuntime actual = runtime;
            Term reference = owned
                ? await actor.InvokeAsync(async c => await actual.Modules.Call(
                    c,
                    AtomicsHost413Names.Internal,
                    AtomicsHost413Names.Constructor,
                    Term.I(1),
                    Term.I(1)
                ))
                : await AtomicsHost413Access.Call(
                    actor,
                    runtime,
                    AtomicsHost413Names.New,
                    Term.I(1),
                    Nil.Value
                );
            if (owned)
            {
                object heap = typeof(ProcessContext).GetProperty(Heap, Members)!.GetValue(context)!;
                frame = (IDisposable)Invoke(heap, Enter);
                int slot = (int)Invoke(frame, Reserve);
                Invoke(
                    frame,
                    Set,
                    slot,
                    reference
                );
            }
            held = Hold(runtime, reference, invocation);
            shutdown = Task.Run(async () =>
            {
                if (halt)
                {
                    await actual.RequestHalt(0);
                    await actual.HaltQuiescence;
                }
                else
                    await actual.DisposeAsync();
            });
            using var deadline = new CancellationTokenSource(Deadline);
            while (!runtime.AtomicsStorage413.Closed)
                await Task.Delay(1, deadline.Token);
            Require(!shutdown.IsCompleted);
            object gate = typeof(ProcessRuntime).GetField(Gate, Members)!.GetValue(runtime)!;
            bool entered = await Task.Run(() =>
            {
                bool acquired = Monitor.TryEnter(gate, Probe);
                if (acquired)
                    Monitor.Exit(gate);

                return acquired;
            }).WaitAsync(Deadline);
            if (!owned)
                Require(entered);
            held.Dispose();
            held = null;
            if (owned)
            {
                // A real admitted frame keeps its actor heap physically alive.
                // Close admission is not permission to free its backing early.
                await Task.WhenAny(shutdown, Task.Delay(Probe));
                Require(runtime.AtomicsStorage413.LiveArrays == 1 && !shutdown.IsCompleted);
                frame!.Dispose();
                frame = null;
            }
            await shutdown.WaitAsync(Deadline);
            if (halt)
                await runtime.HaltQuiescence.WaitAsync(Deadline);
            Require(runtime.AtomicsStorage413.LiveArrays == 0
                && runtime.AtomicsStorage413.ActiveInvocations == 0
                && runtime.AtomicsStorage413.OwnedNativeBytes == 0);
            GC.KeepAlive(reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            Attempt(() => held?.Dispose(), failures);
            Attempt(() => frame?.Dispose(), failures);
            if (shutdown is not null)
                await Drain(() => shutdown, failures);
            if (actor is not null)
                await Drain(() => actor.DisposeAsync().AsTask(), failures);
            if (runtime is not null)
                await Drain(() => runtime.DisposeAsync().AsTask(), failures);
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }

    private static void Attempt(Action action, List<Exception> failures)
    {
        try
        {
            action();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
    }

    private static async Task Drain(Func<Task> action, List<Exception> failures)
    {
        try
        {
            await Task.Run(action).WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
    }
}
