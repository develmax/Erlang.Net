using Erlang;
using System.Reflection;
using static Erlang.Tests.MailboxSnapshotSettlement593Names;

namespace Erlang.Tests;

// Component controls use finite disposable roots, not synthetic C destructors.
// All source-only until root integrates/builds/runs the complete route packet.
internal static class MailboxSnapshotSettlement593Checks
{
    internal static async Task SharedSnapshotFinalRecipientSettlesActualRoots()
    {
        var runtime = new ProcessRuntime();
        object owner = Create(Owner), first = Create(Tracker), second = Create(Tracker);
        var probe = new Probe();
        object origin = RegisterOrigin(owner, probe);
        object graph = Activator.CreateInstance(
            Type(Snapshot),
            Members,
            null,
            new[] { runtime, origin },
            null
        )!;
        object firstOutcome = Call(
            owner,
            Fork,
            runtime,
            graph,
            first
        )!;
        object secondOutcome = Call(
            owner,
            Fork,
            runtime,
            graph,
            second
        )!;
        object firstWork = firstOutcome.GetType().GetProperty(AdmittedProperty, Members)!.GetValue(firstOutcome)!;
        object secondWork = secondOutcome.GetType().GetProperty(AdmittedProperty, Members)!.GetValue(secondOutcome)!;
        var failures = new List<Exception>();
        try
        {
            await ((IAsyncDisposable)graph).DisposeAsync();
            Arm(firstWork, null);
            await ((Task)Call(first, Drain)!).WaitAsync(Deadline);
            Verify(probe.Count == 0, OtherRecipient);
            Arm(secondWork, null);
            await ((Task)Call(second, Drain)!).WaitAsync(Deadline);
            Verify(probe.Count == 1, FinalRoot);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            // Each independent cleanup runs even if an earlier assertion failed.
            await Cleanup(async () => await ((IAsyncDisposable)graph).DisposeAsync(), failures);
            await Cleanup(async () =>
 {
     Arm(firstWork, null);
     await ((Task)Call(first, Drain)!).WaitAsync(Deadline);
 }, failures);
            await Cleanup(async () =>
 {
     Arm(secondWork, null);
     await ((Task)Call(second, Drain)!).WaitAsync(Deadline);
 }, failures);
            await Cleanup(async () => await ((Task)Call(owner, Drain)!).WaitAsync(Deadline), failures);
            await Cleanup(async () => await runtime.DisposeAsync().AsTask().WaitAsync(Deadline), failures);
        }

        if (failures.Count != 0) throw new AggregateException(failures);
    }

    internal static async Task SpecificReadyRootFallbackRetainsFailure()
    {
        object owner = Create(Owner);
        var probe = new Probe();
        object work = RegisterOrigin(owner, probe);
        var marker = new InvalidOperationException(Marker);
        // Construct the exact Ready-but-not-queued boundary. This does not
        // claim to induce a real CLR ThreadPool outage.
        Arm(work, marker);
        Exception? observed = null;
        try
        {
            var completion = (ValueTask)Type(Settlement).GetMethod(Settle, Members)!.Invoke(null, new[] { work })!;
            await completion.AsTask().WaitAsync(Deadline);
        }
        catch (Exception failure)
        {
            observed = failure;
        }

        Verify(probe.Count == 1 && Contains(observed, marker), FailedFallback);
        try
        {
            await ((Task)Call(owner, Drain)!).WaitAsync(Deadline);
        }
        catch (AggregateException failure) when (failure.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, marker))) { }
        Verify(probe.Count == 1, FinalRoot);
    }

    private static bool Contains(Exception? value, Exception selected)
    {
        if (ReferenceEquals(value, selected)) return true;
        if (value is AggregateException aggregate) return aggregate.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, selected));

        return value?.InnerException is { } inner && Contains(inner, selected);
    }

    private static object RegisterOrigin(object owner, IDisposable probe)
    {
        object work = Activator.CreateInstance(
            Type(Work),
            Members,
            null,
            new[] { owner },
            null
        )!;
        object list = Field(owner, Active)!;
        object link = Field(work, Link)!;
        list.GetType().GetMethods().Single(method => method.Name == AddLast && method.GetParameters().Length == 1 &&
            method.GetParameters()[0].ParameterType == link.GetType()).Invoke(list, new[] { link });
        Call(work, Attach, probe);

        return work;
    }

    private static void Arm(object work, Exception? failure)
    {
        Set(work, References, 0);
        Set(work, OwnerReleased, 1);
        Set(work, QueueFailure, failure);
        Set(work, QueueSettled, 1);
        Set(work, Ready, 1);
    }

    private static Type Type(string name) => typeof(ProcessRuntime).Assembly.GetType(name, true)!;

    private static object Create(string name) => Activator.CreateInstance(Type(name), true)!;

    private static object? Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target);

    private static void Set(object target, string name, object? value) => target.GetType().GetField(name, Members)!.SetValue(target, value);

    private static object? Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Members)!.Invoke(target, args);

    private static void Verify(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Cleanup(Func<Task> action, List<Exception> failures)
    {
        try
        {
            await action();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
    }

    private sealed class Probe : IDisposable
    {
        internal int Count;

        public void Dispose() => Interlocked.Increment(ref Count);
    }
}
