using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MailboxSnapshot601TestNames;
using static Erlang.Tests.ProcessInfoLate601Names;

namespace Erlang.Tests;

internal static class ProcessInfoLate601Checks
{
    internal static Task CancelAfterPublication() => AfterPublication(true);

    internal static Task ExitAfterCapture() => AfterPublication(false);

    internal static Task ExitBeforeCapture() => MailboxSnapshot601Fixture.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? requester = null;
        Action? restore = null;
        try
        {
            var enqueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            restore = Observe(fixture.MailboxOwner, enqueued);
            requester = new(fixture.Runtime);
            await requester.Ready.WaitAsync(Deadline);
            Task<Term> query = requester.InvokeAsync(async context =>
                await fixture.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    fixture.TargetContext.Self,
                    Term.A(Messages)
                ));
            await enqueued.Task.WaitAsync(Deadline);
            // Target command loop exits without admitting the pending callback.
            await fixture.Target.DisposeAsync().AsTask().WaitAsync(Deadline);
            Term actual = await query.WaitAsync(Deadline);
            MailboxSnapshot601Fixture.Require(actual.Equals(Term.A(ProcessRegistryAtoms.Undefined)));
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
                restore?.Invoke();
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

    private static Task AfterPublication(bool cancel) => MailboxSnapshot601Fixture.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? requester = null, controller = null;
        Action? restore = null;
        ProcessContext? caller = null;
        Task<Term>? query = null;
        bool callerCancelled = false;
        try
        {
            var enqueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            restore = Observe(fixture.MailboxOwner, enqueued);
            requester = new(fixture.Runtime);
            controller = new(fixture.Runtime);
            caller = await requester.Ready.WaitAsync(Deadline);
            await controller.Ready.WaitAsync(Deadline);
            query = requester.InvokeAsync(async context =>
            {
                using var host = new NativeTermHostScope524(fixture.Runtime);
                Term result = await fixture.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    fixture.TargetContext.Self,
                    Term.A(Messages)
                );
                Term[] messages = Cons.Items(((TupleTerm)result).Items[1]).ToArray();
                MailboxSnapshot601Fixture.Require(messages.Length == 1 && messages[0] is ResourceReferenceTerm);
                Console.WriteLine(PhaseObservation, ReplyReturnedPhase);
                fixture.Adopt(messages[0]);
                Console.WriteLine(PhaseObservation, DestinationAdoptedPhase);
                // Public Call's temporary owned result frame has now ended.
                // Reclaim its unreachable header while Host/destination custody
                // still protects the actual native allocation.
                Call(
                    Property(context, Heap),
                    Major,
                    true,
                    ushort.MaxValue
                );
                Console.WriteLine(PhaseObservation, RequesterCollectedPhase);

                return (Term)Nil.Value;
            });
            await enqueued.Task.WaitAsync(Deadline);
            // Existing real asynchronous suspension is installed AFTER query
            // enqueue, before target admission; no private scheduler state flips.
            Term suspended = await controller.InvokeAsync(async context =>
                await fixture.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    Suspend,
                    caller.Self,
                    Term.List(Term.A(Asynchronous))
                ));
            MailboxSnapshot601Fixture.Require(suspended.Equals(Term.A(ErlangBooleanAtoms.True)));
            await fixture.Target.Invoke(context =>
 {
     Call(fixture.Runtime, AdmitSignals, context);

     return Nil.Value;
 });
            object mailbox = Property(caller, MailboxProperty);
            IDisposable captured = (IDisposable)Call(mailbox, CaptureSnapshot)!;
            Exception? captureFailure = null;
            try
            {
                Term[] values = ((IEnumerable<Term>)Property(captured, Values)).ToArray();
                MailboxSnapshot601Fixture.Require(values.Length == 1
                    && values[0] is TupleTerm { Items.Count: 2 } reply && reply.Items[0] is ReferenceTerm
                    && reply.Items[1] is TupleTerm { Items.Count: 2 } metadata
                    && metadata.Items[0].Equals(Term.A(Messages))
                    && Cons.Items(metadata.Items[1]).Single() is ResourceReferenceTerm);
            }
            catch (Exception error)
            {
                captureFailure = error;
                throw;
            }
            finally
            {
                try
                {
                    captured.Dispose();
                }
                catch (Exception cleanup) when (captureFailure is not null)
                {
                    throw new AggregateException(captureFailure, cleanup);
                }
            }
            MailboxSnapshot601Fixture.Require(!query.IsCompleted);
            // Logical/physical target completion must not join a requester's
            // independent snapshot-origin interest. It must finish first.
            await fixture.Target.DisposeAsync().AsTask().WaitAsync(Deadline);
            MailboxSnapshot601Fixture.Require(!query.IsCompleted && !((Task)Property(fixture.Payload, Retired)).IsCompleted);
            if (cancel)
            {
                await ((CancellationTokenSource)Property(caller, StopSource)).CancelAsync();
                callerCancelled = true;
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
                await ((Task)Property(fixture.Payload, Retired)).WaitAsync(Deadline);
            }
            else
            {
                Term resumed = await controller.InvokeAsync(async context =>
                    await fixture.Runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        Resume,
                        caller.Self
                    ));
                MailboxSnapshot601Fixture.Require(resumed.Equals(Term.A(ErlangBooleanAtoms.True)));
                Term result = await query.WaitAsync(Deadline);
                MailboxSnapshot601Fixture.Require(result is Nil && !((Task)Property(fixture.Payload, Retired)).IsCompleted);
                Console.WriteLine(PhaseObservation, DestinationRetirementPhase);
                await fixture.VerifyAndRetire();
            }
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
            if (caller is not null && query is { IsCompleted: false } && !callerCancelled)
                try
                {
                    await ((CancellationTokenSource)Property(caller, StopSource)).CancelAsync();
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
        }
        Finish(failures);
    });

    private static Action Observe(object mailbox, TaskCompletionSource ready)
    {
        PropertyInfo property = mailbox.GetType().GetProperty(PendingSignal, Members)!;
        Delegate? previous = (Delegate?)property.GetValue(mailbox);
        Type argument = property.PropertyType.GetGenericArguments()[0];
        Delegate observed = (Delegate)typeof(ProcessInfoLate601Checks).GetMethod(nameof(Wrap), Members)!
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

    private static void Finish(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }
}
