using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MailboxSnapshot601TestNames;

namespace Erlang.Tests;

internal static class ProcessInfo601ProtocolChecks
{
    internal static async Task Mutual()
    {
        var runtime = new ProcessRuntime();
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? first = null, second = null;
        try
        {
            first = new(runtime);
            second = new(runtime);
            ProcessContext a = await first.Ready.WaitAsync(Deadline);
            ProcessContext b = await second.Ready.WaitAsync(Deadline);
            var both = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int started = 0;

            async Task<Term> QueryOther(ProcessContext caller, Pid target)
            {
                if (Interlocked.Increment(ref started) == 2) both.TrySetResult();
                await both.Task.WaitAsync(Deadline);
                // Direct real protocol method intentionally avoids a module
                // entry's incidental charge pumping the other pending request.
                Type itemType = typeof(ProcessRuntime).Assembly.GetType(ItemType, true)!;
                object item = itemType.GetMethod(Parse, Members)!.Invoke(null, [Term.A(Messages)])!;
                Array keys = Array.CreateInstance(itemType, 1);
                keys.SetValue(item, 0);
                var pending = (ValueTask<Term>)Call(
                    runtime,
                    Query,
                    caller,
                    target,
                    keys,
                    true
                )!;

                return await pending;
            }
            Task<Term> left = first.InvokeAsync(context => QueryOther(context, b.Self));
            Task<Term> right = second.InvokeAsync(context => QueryOther(context, a.Self));
            Term[] results = await Task.WhenAll(left, right).WaitAsync(Deadline);
            foreach (Term result in results)
            {
                MailboxSnapshot601Fixture.Require(result is TupleTerm { Items.Count: 2 } tuple
                    && tuple.Items[0].Equals(Term.A(Messages)));
                _ = Cons.Items(((TupleTerm)result).Items[1]).ToArray();
            }
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            // Outside actors: cancellation first also bounds the actual RED.
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (first is not null)
                try
                {
                    await first.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (second is not null)
                try
                {
                    await second.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        Finish(failures);
    }

    internal static async Task ReplyOrdering()
    {
        var runtime = new ProcessRuntime();
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? target = null, requester = null;
        IDisposable? captured = null;
        PropertyInfo? pendingProperty = null;
        object? targetMailbox = null;
        Delegate? previous = null;
        try
        {
            target = new(runtime);
            requester = new(runtime);
            ProcessContext targetContext = await target.Ready.WaitAsync(Deadline);
            ProcessContext caller = await requester.Ready.WaitAsync(Deadline);
            targetMailbox = Property(targetContext, MailboxProperty);
            object callerMailbox = Property(caller, MailboxProperty);
            runtime.Send(targetContext.Self, Term.I(DefaultMessage));
            var pendingReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            pendingProperty = targetMailbox.GetType().GetProperty(PendingSignal, Members)!;
            previous = (Delegate?)pendingProperty.GetValue(targetMailbox);
            Type argument = pendingProperty.PropertyType.GetGenericArguments()[0];
            Delegate observed = (Delegate)typeof(ProcessInfo601ProtocolChecks)
                .GetMethod(nameof(ObservePending), Members)!.MakeGenericMethod(argument)
                .Invoke(null, [previous, pendingReady])!;
            pendingProperty.SetValue(targetMailbox, observed);
            Task<Term> query = requester.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    targetContext.Self,
                    Term.A(Messages)
                ));
            await pendingReady.Task.WaitAsync(Deadline);
            object runtimeGate = typeof(ProcessRuntime).GetField(RuntimeGate, Members)!.GetValue(runtime)!;
            object mailboxGate = callerMailbox.GetType().GetField(MailboxGate, Members)!.GetValue(callerMailbox)!;
            await target.Invoke(context =>
            {
                // Existing canonical R->M order; receiver cannot consume while
                // this controlled barrier records the actual queue position.
                lock (runtimeGate)
                    lock (mailboxGate)
                    {
                        Call(runtime, AdmitSignals, context);
                        runtime.Send(caller.Self, Term.A(AfterReply));
                        captured = (IDisposable)Call(callerMailbox, CaptureSnapshot)!;
                    }

                return Nil.Value;
            });
            var values = ((IEnumerable<Term>)Property(captured!, Values)).ToArray();
            Term expected = Term.Tuple(Term.A(Messages), Term.List(Term.I(DefaultMessage)));
            MailboxSnapshot601Fixture.Require(values.Length == 2
                && values[0] is TupleTerm { Items.Count: 2 } reply && reply.Items[0] is ReferenceTerm
                && reply.Items[1].Equals(expected) && values[1].Equals(Term.A(AfterReply)));
            (captured ?? throw new InvalidOperationException(Failure)).Dispose();
            captured = null;
            MailboxSnapshot601Fixture.Require((await query.WaitAsync(Deadline)).Equals(expected));
            Term after = await requester.InvokeAsync(async context =>
                (await context.ReceiveAsync<Term>(value => value.Equals(Term.A(AfterReply)) ? value : null, Deadline))!);
            MailboxSnapshot601Fixture.Require(after.Equals(Term.A(AfterReply)));
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                if (pendingProperty is not null && targetMailbox is not null) pendingProperty.SetValue(targetMailbox, previous);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                captured?.Dispose();
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
        }
        Finish(failures);
    }

    internal static Task Cancellation() => MailboxSnapshot601Fixture.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? requester = null;
        PropertyInfo pendingProperty = fixture.MailboxOwner.GetType().GetProperty(PendingSignal, Members)!;
        Delegate? previous = (Delegate?)pendingProperty.GetValue(fixture.MailboxOwner);
        try
        {
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Type argument = pendingProperty.PropertyType.GetGenericArguments()[0];
            Delegate observed = (Delegate)typeof(ProcessInfo601ProtocolChecks)
                .GetMethod(nameof(ObservePending), Members)!.MakeGenericMethod(argument)
                .Invoke(null, [previous, ready])!;
            pendingProperty.SetValue(fixture.MailboxOwner, observed);
            requester = new(fixture.Runtime);
            ProcessContext context = await requester.Ready.WaitAsync(Deadline);
            Task<Term> query = requester.InvokeAsync(async active =>
                await fixture.Runtime.Modules.Call(
                    active,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    fixture.TargetContext.Self,
                    Term.A(Messages)
                ));
            await ready.Task.WaitAsync(Deadline);
            // No target admission has run: actual cancellation must unlink the
            // request and release its independently held producer/capture state.
            await ((CancellationTokenSource)Property(context, StopSource)).CancelAsync();
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
            await fixture.Target.Invoke(active =>
 {
     Call(fixture.Runtime, AdmitSignals, active);

     return Nil.Value;
 });
            await fixture.Dequeue();
            await ((Task)Property(fixture.Payload, Retired)).WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                pendingProperty.SetValue(fixture.MailboxOwner, previous);
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
        }
        Finish(failures);
    });

    private static Delegate ObservePending<T>(Action<T>? previous, TaskCompletionSource ready)
        => new Action<T>(value =>
 {
     previous?.Invoke(value);
     ready.TrySetResult();
 });

    private static void Finish(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }
}
