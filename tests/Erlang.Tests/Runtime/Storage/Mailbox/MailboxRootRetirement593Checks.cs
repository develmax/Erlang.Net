using System.Reflection;
using Erlang;
using static Erlang.Tests.MailboxRootRetirement593Names;

namespace Erlang.Tests;

// Privileged finite post-scheduling-boundary controls. They do not simulate a
// real CLR ThreadPool outage or claim actual C destructor/array-free settlement.
internal static class MailboxRootRetirement593Checks
{
    internal static async Task RecipientFallbackAndErrors()
    {
        object owner = Create(Owner);
        object recipient = Create(Tracker);
        var queueError = new InvalidOperationException(QueueMarker);
        var releaseError = new InvalidOperationException(ReleaseMarker);
        var probe = new Probe(releaseError);
        object work = Register(owner, recipient, probe);
        ArmReadyWithoutQueue(work, queueError);
        Call(recipient, Close);

        Exception? observed = null;
        try
        {
            await ((Task)Call(recipient, Drain)!).WaitAsync(Deadline);
        }
        catch (Exception failure)
        {
            observed = failure;
        }

        Verify(probe.Count == 1, DisposedCount);
        Verify(
            observed is AggregateException errors &&
            errors.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, queueError)) &&
            errors.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, releaseError)),
            MissingFailure
        );

        // The runtime owner must independently retain exactly the same errors.
        Exception? global = null;
        try
        {
            await ((Task)Call(owner, Drain)!).WaitAsync(Deadline);
        }
        catch (Exception failure)
        {
            global = failure;
        }

        Verify(probe.Count == 1, DisposedCount);
        Verify(
            global is AggregateException retained &&
            retained.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, queueError)) &&
            retained.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, releaseError)),
            MissingFailure
        );
    }

    internal static async Task RecipientDrainDoesNotJoinOtherMailbox()
    {
        object owner = Create(Owner);
        object first = Create(Tracker), second = Create(Tracker);
        var firstProbe = new Probe();
        var secondProbe = new Probe();
        object firstWork = Register(owner, first, firstProbe);
        object secondWork = Register(owner, second, secondProbe);
        try
        {
            ArmReadyWithoutQueue(firstWork, null);
            Call(first, Close);
            await ((Task)Call(first, Drain)!).WaitAsync(Deadline);
            Verify(firstProbe.Count == 1 && secondProbe.Count == 0, UnrelatedWork);
        }
        finally
        {
            // Cleanup is independent of the first recipient assertion and no
            // global owner join starts until the second native root is ready.
            ArmReadyWithoutQueue(secondWork, null);
            Call(second, Close);
            await ((Task)Call(second, Drain)!).WaitAsync(Deadline);
            await ((Task)Call(owner, Drain)!).WaitAsync(Deadline);
        }

        Verify(firstProbe.Count == 1 && secondProbe.Count == 1, DisposedCount);
    }

    private static object Register(object owner, object tracker, Probe probe)
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(Work, true)!;
        object work = Activator.CreateInstance(
            type,
            Members,
            null,
            new[] { owner },
            null
        )!;
        object list = Field(owner, Active)!;
        object link = Field(work, Link)!;
        MethodInfo add = list.GetType().GetMethods().Single(method => method.Name == AddLast &&
            method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == link.GetType());
        add.Invoke(list, new[] { link });
        Call(work, Attach, probe);
        Call(work, BindRecipient, tracker);

        return work;
    }

    private static void ArmReadyWithoutQueue(object work, Exception? failure)
    {
        Set(work, References, 0);
        Set(work, OwnerReleased, 1);
        Set(work, QueueFailure, failure);
        Set(work, QueueSettled, 1);
        Set(work, Ready, 1);
    }

    private static object Create(string name) => Activator.CreateInstance(typeof(ProcessRuntime).Assembly.GetType(name, true)!, true)!;

    private static object? Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target);

    private static void Set(object target, string name, object? value) => target.GetType().GetField(name, Members)!.SetValue(target, value);

    private static object? Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Members)!.Invoke(target, args);

    private static void Verify(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Probe(Exception? failure = null) : IDisposable
    {
        internal int Count;

        public void Dispose()
        {
            Interlocked.Increment(ref Count);
            if (failure is not null) throw failure;
        }
    }
}
