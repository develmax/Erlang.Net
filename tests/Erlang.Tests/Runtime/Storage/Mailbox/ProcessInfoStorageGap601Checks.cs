using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MailboxSnapshot601TestNames;
using static Erlang.Tests.ProcessInfoStorageGap601Names;

namespace Erlang.Tests;

internal static class ProcessInfoStorageGap601Checks
{
    internal static async Task Run()
    {
        var runtime = new ProcessRuntime(ActorConcurrency);
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? target = null, requester = null, controller = null;
        ProcessContext? targetContext = null;
        IDisposable? lease = null;
        object? request = null;
        Task? blocker = null;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Action? restore = null;
        try
        {
            target = new(runtime);
            requester = new(runtime);
            controller = new(runtime);
            targetContext = await target.Ready.WaitAsync(Deadline);
            ProcessContext caller = await requester.Ready.WaitAsync(Deadline);
            await controller.Ready.WaitAsync(Deadline);
            object storage = EtsFoundation405Access.Get(runtime);
            lease = await target.Invoke(context =>
            {
                Term table = EtsFoundation405Access.Create(storage, context);
                long slot = EtsFoundation405Access.Store(
                    storage,
                    context,
                    table,
                    Term.Tuple(Term.I(StoredKey))
                );

                return (IDisposable)EtsFoundation405Access.Invoke(
                    storage,
                    EtsFoundation405Names.AcquireRead,
                    context,
                    table,
                    slot
                )!;
            });
            object stored = lease.GetType().GetField(ReadOwner, Members)!.GetValue(lease)!;
            object storedGate = stored.GetType().GetField(StoredGate, Members)!.GetValue(stored)!;
            var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            // The helper keeps one real stored-object monitor across a finite
            // barrier; it NEVER holds runtime.gate, so Queryfinally can proceed.
            blocker = Task.Run(() =>
            {
                lock (storedGate)
                {
                    held.TrySetResult();
                    release.Task.WaitAsync(Deadline).GetAwaiter().GetResult();
                }
            });
            await held.Task.WaitAsync(Deadline);
            object mailbox = Property(targetContext, MailboxProperty);
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            restore = Observe(mailbox, pending);
            Task<Term> query = requester.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    targetContext.Self,
                    Term.A(Messages)
                ));
            await pending.Task.WaitAsync(Deadline);
            object runtimeGate = typeof(ProcessRuntime).GetField(RuntimeGate, Members)!.GetValue(runtime)!;
            object requests = Property(targetContext, PendingRequests);
            lock (runtimeGate) request = ((IEnumerable)requests).Cast<object>().Single();
            await controller.Invoke(context =>
            {
                runtime.Exit(context, targetContext.Self, Term.A(Kill));

                return Nil.Value;
            });
            // Actual Terminate removed the process, but real ETS retirement
            // cannot Release its row until the stored-object barrier is lifted.
            MailboxSnapshot601Fixture.Require(!runtime.IsAlive(targetContext.Self) && !target.Process.Completion.IsCompleted);
            await ((CancellationTokenSource)Property(caller, StopSource)).CancelAsync();
            bool cancelled = false;
            try
            {
                await query.WaitAsync(Deadline);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            MailboxSnapshot601Fixture.Require(cancelled);
            await requester.DisposeAsync().AsTask().WaitAsync(Deadline);
            requester = null;
            lock (runtimeGate)
                MailboxSnapshot601Fixture.Require((int)Property(requests, Count) == 0);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                restore?.Invoke();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            release.TrySetResult();
            if (blocker is not null)
                try
                {
                    await blocker.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (target is not null)
                try
                {
                    await target.Process.Completion.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            // Observe the real late producer BEFORE diagnostic RED recovery.
            try
            {
                object roots = Property(runtime, MailboxRoots);
                object gate = roots.GetType().GetField(OwnerGate, Members)!.GetValue(roots)!;
                lock (gate)
                {
                    object active = roots.GetType().GetField(ActiveRoots, Members)!.GetValue(roots)!;
                    MailboxSnapshot601Fixture.Require((int)Property(active, Count) == 0);
                }
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            // A failing RED must not hang global shutdown. This exact known
            // request recovery happens only AFTER both failed assertions have
            // been preserved. It is not production cleanup or a result filter.
            if (failures.Count != 0 && request is not null)
                try
                {
                    typeof(ProcessRuntime).GetMethod(ReleaseProducer, Members)!.Invoke(null, [request]);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                lease?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (controller is not null)
                try
                {
                    await controller.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (requester is not null)
                try
                {
                    await requester.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (target is not null)
                try
                {
                    await target.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }

    private static Action Observe(object mailbox, TaskCompletionSource ready)
    {
        PropertyInfo property = mailbox.GetType().GetProperty(PendingSignal, Members)!;
        Delegate? previous = (Delegate?)property.GetValue(mailbox);
        Type argument = property.PropertyType.GetGenericArguments()[0];
        Delegate observed = (Delegate)typeof(ProcessInfoStorageGap601Checks).GetMethod(nameof(Wrap), Members)!
            .MakeGenericMethod(argument).Invoke(null, [previous, ready])!;
        property.SetValue(mailbox, observed);

        return () => property.SetValue(mailbox, previous);
    }

    private static Delegate Wrap<T>(Action<T>? previous, TaskCompletionSource ready)
        => new Action<T>(value =>
 {
     previous?.Invoke(value);
     ready.TrySetResult();
 });
}
