using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MailboxBulkClose606Names;

namespace Erlang.Tests;

// Actual queue/work ownership control. The disposable group is a finite probe,
// not a NIF destructor or a claim of executing a complete native resource family.
internal static class MailboxBulkClose606Checks
{
    internal static async Task Run()
    {
        var failures = new List<Exception>();
        foreach (bool directHalt in new[] { false, true })
        {
            var runtime = new ProcessRuntime();
            RuntimeNodeFoundationHostActor? actor = null;
            Mailbox? mailbox = null;
            PropertyInfo? observer = null;
            Delegate? previous = null;
            object? graph = null;
            IDisposable? publication = null;
            var probe = new Probe();
            try
            {
                actor = new(runtime);
                ProcessContext context = await actor.Ready.WaitAsync(Deadline);
                mailbox = context.Mailbox;
                object rootOwner = Property(runtime, MailboxOpaqueRemove601Names.Owner);
                graph = Call(
                    rootOwner,
                    MailboxOpaqueRemove601Names.Prepare,
                    runtime,
                    probe
                )!;
                Type scope = typeof(ProcessRuntime).Assembly.GetType(MailboxOpaqueRemove601Names.Scope, true)!;
                publication = (IDisposable)scope.GetMethod(MailboxOpaqueRemove601Names.Forward, Members)!
                    .Invoke(null, [runtime, graph])!;
                MethodInfo send = typeof(Mailbox).GetMethod(MailboxOpaqueRemove601Names.Send, Members)!;
                send.Invoke(mailbox, [Term.I(1), null, false, false, null, false, null, publication]);
                send.Invoke(mailbox, [Term.I(2), null, false, false, null, false, null, publication]);
                publication.Dispose();
                publication = null;
                await ((IAsyncDisposable)graph).DisposeAsync();
                observer = typeof(Mailbox).GetProperty(QueueObserver, Members)!;
                previous = (Delegate?)observer.GetValue(mailbox);
                var counts = new List<int>();
                int predicates = 0, removed = -1;
                await actor.Invoke(active =>
                {
                    Action<int> hook = count =>
                    {
                        (previous as Action<int>)?.Invoke(count);
                        counts.Add(count);
                        if (count == 1)
                        {
                            if (directHalt) Call(mailbox, Stop);
                            runtime.Exit(active, active.Self, Term.A(Kill));
                        }
                    };
                    observer.SetValue(mailbox, hook);
                    removed = mailbox.Remove(_ =>
 {
     predicates++;

     return true;
 });

                    return Nil.Value;
                }).WaitAsync(Deadline);
                Term reason = await actor.Process.Completion.WaitAsync(Deadline);
                Require(reason.Equals(Term.A(Killed)) && !runtime.IsAlive(context.Self));
                // Both opaque predicates were prepared/invoked before commit.
                // Close retired the second Entry: it is not another explicit remove.
                Require(predicates == 2 && removed == 1 && counts.SequenceEqual(new[] { 1 }));
                Require(mailbox.Count == 0 && probe.Count == 1);
                GC.KeepAlive(graph);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            finally
            {
                try
                {
                    if (observer is not null && mailbox is not null) observer.SetValue(mailbox, previous);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                try
                {
                    publication?.Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                if (graph is IAsyncDisposable captured)
                    try
                    {
                        await captured.DisposeAsync().AsTask().WaitAsync(Deadline);
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
                if (actor is not null)
                    try
                    {
                        await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
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
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count != 0) throw new AggregateException(Failure, failures);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    private sealed class Probe : IDisposable
    {
        internal int Count;

        public void Dispose() => Interlocked.Increment(ref Count);
    }
}
