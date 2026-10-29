using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.CountersExit603Names;

namespace Erlang.Tests;

internal static class CountersExit603Checks
{
    internal static async Task InitializerDoneBeforeGlobalDrain()
    {
        var failures = new List<Exception>();
        var fixture = new CountersCustody596Fixture();
        RuntimeNodeFoundationHostActor? child = null;
        RuntimeNodeFoundationHostActor? killer = null;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finallyRan = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recovery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool>? childCommand = null;
        try
        {
            await fixture.Runtime.InitializeModulesAsync(_ => { });
            IDisposable retained = fixture.Lease(CountersCustody596Names.GuestKind);
            fixture.ReleaseProducer();
            Task initializerDone = InitializerDone(fixture.Runtime);
            child = new(fixture.Runtime);
            killer = new(fixture.Runtime);
            childCommand = child.InvokeAsync(async context =>
            {
                // A real owned invocation/header remains admitted across this
                // wait; a CLR-held ReferenceTerm alone would not reproduce it.
                using var invocation = (IDisposable)Call(
                    context,
                    Scope,
                    (IReadOnlyList<Term>)new Term[] { fixture.Reference },
                    false
                )!;
                var arguments = (Term[])Property(invocation, Arguments);
                Require(arguments[0] is ReferenceTerm reference && reference.Words.Count == CountersMagic596Names.Words);
                ready.TrySetResult();
                try
                {
                    Task winner = await Task.WhenAny(initializerDone, recovery.Task);
                    if (!ReferenceEquals(winner, initializerDone))
                        return false; // HOST-only recovery in test finally.
                    await initializerDone;

                    return true;
                }
                finally
                {
                    invocation.Dispose();
                    finallyRan.TrySetResult();
                }
            });
            await ready.Task.WaitAsync(CountersMagic596Names.Deadline);
            await killer.Invoke(context =>
            {
                fixture.Runtime.Exit(context, fixture.Runtime.InitializationPid, Term.A(Kill));

                return true;
            });
            await initializerDone.WaitAsync(CountersMagic596Names.Deadline);
            Require(await childCommand.WaitAsync(CountersMagic596Names.Deadline));
            await finallyRan.Task.WaitAsync(CountersMagic596Names.Deadline);
            // Explicit Guest custody still blocks physical backing retirement.
            // Init death is whole-VM shutdown, so no usable-VM claim belongs here.
            Require((bool)Property(fixture.Array, CountersCustody596Names.Allocated));
            Require(!((Task)Property(fixture.Array, CountersCustody596Names.Retired)).IsCompleted);
            retained.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            // Do not leave a deliberately reproduced old cycle unbounded.
            // This signal is never sent on the assertion path.
            recovery.TrySetResult();
            if (childCommand is not null)
                await Cleanup(async () =>
 {
     _ = await childCommand.WaitAsync(CountersMagic596Names.Deadline);
 }, failures);
            await fixture.Drain(failures);
            if (child is not null)
                await Cleanup(() => child.DisposeAsync().AsTask(), failures);
            if (killer is not null)
                await Cleanup(() => killer.DisposeAsync().AsTask(), failures);
        }
        Throw(failures);
    }

    internal static async Task OrdinaryExitPreservesIndependentHeap()
    {
        var failures = new List<Exception>();
        var fixture = new CountersCustody596Fixture();
        RuntimeNodeFoundationHostActor? ownerActor = null;
        RuntimeNodeFoundationHostActor? remaining = null;
        IDisposable? independentFrame = null;
        try
        {
            await fixture.Runtime.InitializeModulesAsync(_ => { });
            IDisposable retained = fixture.Lease(CountersCustody596Names.GuestKind);
            ownerActor = new(fixture.Runtime);
            remaining = new(fixture.Runtime);
            ProcessContext remainingContext = await remaining.Ready.WaitAsync(CountersMagic596Names.Deadline);
            object heap = Property(remainingContext, CountersMagic596Names.Heap);
            independentFrame = (IDisposable)Call(heap, CountersMagic596Names.Enter)!;
            int slot = (int)Call(independentFrame, CountersMagic596Names.Reserve)!;
            Call(
                independentFrame,
                CountersMagic596Names.Set,
                slot,
                fixture.Reference
            );
            await ownerActor.Invoke(context =>
            {
                using var invocation = (IDisposable)Call(
                    context,
                    Scope,
                    (IReadOnlyList<Term>)new Term[] { fixture.Reference },
                    false
                )!;
                Require(((Term[])Property(invocation, Arguments))[0] is ReferenceTerm);

                return true;
            });
            fixture.ReleaseProducer();
            retained.Dispose();
            await ownerActor.DisposeAsync().AsTask().WaitAsync(CountersMagic596Names.Deadline);
            ownerActor = null;
            Require(fixture.Runtime.IsAlive(fixture.Runtime.InitializationPid));
            Require(fixture.Runtime.IsAlive(remaining.Process.Pid));
            Require(!fixture.Runtime.CountersStorage421.Closed);
            Require((bool)Property(fixture.Array, CountersCustody596Names.Allocated));
            await remaining.Invoke(context =>
            {
                // Both the independent actual header and backing remain usable.
                var reference = (ReferenceTerm)Call(independentFrame!, CountersMagic596Names.Read, slot)!;
                Require(reference.Words.Count == CountersMagic596Names.Words);
                using var operation = (IDisposable)Call(fixture.Array, CountersCustody596Names.AcquirePin)!;
                Require((ulong)Property(fixture.Array, CountersCustody596Names.OriginalSize) > 0);
                Require((long)Call(
                    fixture.Array,
                    CountersCustody596Names.Cell,
                    0UL,
                    0
                )! == 0);

                return true;
            });
            independentFrame.Dispose();
            independentFrame = null;
            Call(heap, CountersMagic596Names.Collect);
            await ((Task)Property(fixture.Array, CountersCustody596Names.Retired)).WaitAsync(CountersMagic596Names.Deadline);
            Require(!(bool)Property(fixture.Array, CountersCustody596Names.Allocated));
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            try
            {
                independentFrame?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            await fixture.Drain(failures);
            if (ownerActor is not null)
                await Cleanup(() => ownerActor.DisposeAsync().AsTask(), failures);
            if (remaining is not null)
                await Cleanup(() => remaining.DisposeAsync().AsTask(), failures);
        }
        Throw(failures);
    }

    private static Task InitializerDone(ProcessRuntime runtime)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        object gate = typeof(ProcessRuntime).GetField(RuntimeGate, flags)!.GetValue(runtime)!;
        ProcessContext initializer;
        lock (gate)
            initializer = (ProcessContext)((IDictionary)typeof(ProcessRuntime).GetField(Processes, flags)!.GetValue(runtime)!)[runtime.InitializationPid]!;

        return ((TaskCompletionSource<Term>)typeof(ProcessContext).GetProperty(Done, flags)!.GetValue(initializer)!).Task;
    }

    private static async Task Cleanup(Func<Task> operation, List<Exception> failures)
    {
        try
        {
            await operation().WaitAsync(CountersMagic596Names.Deadline);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }

    private static void Throw(List<Exception> failures)
    {
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(CleanupFailure, failures);
    }
}
