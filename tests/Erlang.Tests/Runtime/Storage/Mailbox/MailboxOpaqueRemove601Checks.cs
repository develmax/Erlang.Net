using System.Collections;
using System.Reflection;
using Erlang;
using static Erlang.Tests.MailboxOpaqueRemove601Names;

namespace Erlang.Tests;

// Finite actual queue/operation control; the disposable probe is not a native
// destructor or a claim that the complete resource family was executed.
internal static class MailboxOpaqueRemove601Checks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static async Task BulkCallbackHasNoLaterGuestOperations()
    {
        var runtime = new ProcessRuntime();
        var mailbox = new Mailbox();
        var probe = new Probe();
        object? graph = null;
        IDisposable? publication = null;
        var failures = new List<Exception>();
        try
        {
            typeof(Mailbox).GetProperty(Runtime, Members)!.SetValue(mailbox, runtime);
            object owner = typeof(ProcessRuntime).GetProperty(Owner, Members)!.GetValue(runtime)!;
            graph = owner.GetType().GetMethod(Prepare, Members)!.Invoke(owner, new object[] { runtime, probe })!;
            Type scope = typeof(ProcessRuntime).Assembly.GetType(Scope, true)!;
            publication = (IDisposable)scope.GetMethod(Forward, Members)!.Invoke(null, new[] { runtime, graph })!;
            MethodInfo send = typeof(Mailbox).GetMethod(Send, Members)!;
            send.Invoke(mailbox, new object?[] { Term.I(1), null, false, false, null, false, null, publication });
            send.Invoke(mailbox, new object?[] { Term.I(2), null, false, false, null, false, null, publication });
            object[] works = ((IEnumerable)scope.GetField(Works, Members)!.GetValue(publication)!).Cast<object>().ToArray();
            publication.Dispose();
            publication = null;
            await ((IAsyncDisposable)graph).DisposeAsync();
            int calls = 0;
            int removed = mailbox.Remove(_ =>
            {
                calls++;
                // One original queue owner per entry, no current OR later
                // opaque-candidate operation remains before arbitrary callback.
                if (works.Length != 2 || works.Any(work => (int)work.GetType().GetField(References, Members)!.GetValue(work)! != 1))
                    throw new InvalidOperationException(Failure);

                return true;
            });
            if (calls != 2 || removed != 2) throw new InvalidOperationException(CountFailure);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            await Attempt(() =>
 {
     publication?.Dispose();

     return Task.CompletedTask;
 }, failures);
            await Attempt(async () =>
 {
     if (graph is IAsyncDisposable value) await value.DisposeAsync().AsTask().WaitAsync(Deadline);
 }, failures);
            await Attempt(() =>
 {
     typeof(Mailbox).GetMethod(Close, Members)!.Invoke(mailbox, null);

     return Task.CompletedTask;
 }, failures);
            await Attempt(async () => await ((Task)typeof(Mailbox).GetMethod(Drain, Members)!.Invoke(mailbox, null)!).WaitAsync(Deadline), failures);
            await Attempt(async () => await runtime.DisposeAsync().AsTask().WaitAsync(Deadline), failures);
        }
        if (probe.Count != 1) failures.Add(new InvalidOperationException(CountFailure));
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    private static async Task Attempt(Func<Task> action, List<Exception> failures)
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
