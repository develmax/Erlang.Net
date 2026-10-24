using Erlang;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Linq.Expressions;

namespace Erlang.Tests;

internal static class MailboxReceiveMarkerHostChecks
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    public static Task PositionAndClear() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term before = Term.A(MailboxReceiveMarkerHostNames.BeforeMarker), after = Term.A(MailboxReceiveMarkerHostNames.AfterMarker);
        box.Send(before);
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm reference = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            reference
        );
        box.Send(after);
        Require(box.Count == 2 && Snapshot(box).SequenceEqual(new[] { before, after }));
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, reference);
        Invoke(box, MailboxReceiveMarkerHostNames.ClearReceiveMarker, reference);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(after));
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(before));
        Require(box.Count == 0);
    });

    public static Task RemovalAndExpiration() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term before = Term.A(MailboxReceiveMarkerHostNames.BeforeMarker), after = Term.A(MailboxReceiveMarkerHostNames.AfterMarker);
        box.Send(before);
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm reference = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            reference
        );
        box.Send(after);
        Require(box.RemoveFirst(value => value.Equals(before)));
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, reference);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(after));
        Invoke(box, MailboxReceiveMarkerHostNames.ClearReceiveMarker, reference);
        Require(box.Count == 0);

        box.Send(before);
        ReferenceTerm? oldest = null;
        for (int index = 0; index < 9; index++)
        {
            object next = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
            ReferenceTerm id = runtime.MakeReference();
            oldest ??= id;
            Invoke(
                box,
                MailboxReceiveMarkerHostNames.BindReceiveMarker,
                next,
                id
            );
        }
        Require(MarkerCount(box) == 8);
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, oldest!);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(before));
        Invoke(box, MailboxReceiveMarkerHostNames.StopForHalt);
        Require(MarkerCount(box) == 0 && box.Count == 0 && Snapshot(box).Count == 0);
    });

    public static Task InvalidAndForeign() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term before = Term.A(MailboxReceiveMarkerHostNames.BeforeMarker);
        box.Send(before);
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            Term.A(MailboxReceiveMarkerHostNames.NotAReference)
        );
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, runtime.MakeReference());
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, new ReferenceTerm(MailboxReceiveMarkerHostNames.RemoteHost, 1));
        Invoke(box, MailboxReceiveMarkerHostNames.ClearReceiveMarker, Nil.Value);
        var foreign = new Mailbox();
        Invoke(
            foreign,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            runtime.MakeReference()
        );
        Require(box.Count == 1 && foreign.Count == 0);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(before));
    });

    public static Task PassExpiration() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term before = Term.A(MailboxReceiveMarkerHostNames.BeforeMarker);
        box.Send(before);
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm id = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            id
        );
        for (int index = 0; index < 5; index++)
            Require(await box.ReceiveAsync<Term>(_ => null, TimeSpan.Zero) is null);
        Require(MarkerCount(box) == 0 && box.Count == 1);
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, id);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(before));
    });

    public static Task DuplicateBindingRefused() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term before = Term.A(MailboxReceiveMarkerHostNames.BeforeMarker), middle = Term.A(MailboxReceiveMarkerHostNames.BetweenMarkers), after = Term.A(MailboxReceiveMarkerHostNames.AfterMarker);
        box.Send(before);
        object first = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm reference = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            first,
            reference
        );
        // The insert token was consumed by the first binding: repeated bind is a quiet no-op.
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            first,
            reference
        );
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            first,
            runtime.MakeReference()
        );
        box.Send(middle);
        object second = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        Reject(() => Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            second,
            reference
        ));
        Require(box.Count == 2 && MarkerCount(box) == 2 && Snapshot(box).SequenceEqual(new[] { before, middle }));
        ReferenceTerm different = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            second,
            different
        );
        box.Send(after);
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, reference);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(middle));
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, different);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(after));
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(before));
    });

    public static Task ControlOnlyWait() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        object empty = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            empty,
            runtime.MakeReference()
        );
        Require(MarkerCount(box) == 0);
        box.Send(Term.A(MailboxReceiveMarkerHostNames.BeforeMarker));
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm id = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            id
        );
        Require(box.RemoveFirst(_ => true));
        Require(box.Count == 0 && Snapshot(box).Count == 0 && MarkerCount(box) == 1);
        using var stop = new CancellationTokenSource();
        Task waiting = (Task)Invoke(box, MailboxReceiveMarkerHostNames.WaitForMessage, stop.Token)!;
        Require(!waiting.IsCompleted);
        await stop.CancelAsync();
        try
        {
            await waiting;
            throw new InvalidOperationException(MailboxReceiveMarkerHostNames.ControlOnlyWaitUnexpectedlyCompleted);
        }
        catch (OperationCanceledException)
        {
        }
        Invoke(box, MailboxReceiveMarkerHostNames.ClearReceiveMarker, id);
        Require(MarkerCount(box) == 0);
    });

    public static Task PriorityRefusalBeforeUse() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term before = Term.A(MailboxReceiveMarkerHostNames.BeforeMarker), priority = Term.A(MailboxReceiveMarkerHostNames.PriorityMessage);
        box.Send(before);
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm id = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            id
        );
        box.Send(priority, true);
        Reject(() => Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, id));
        Require(box.Count == 2 && Snapshot(box).SequenceEqual(new[] { priority, before }));
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(priority));
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(before));
    });

    public static Task PriorityRefusalAfterAwait() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term before = Term.A(MailboxReceiveMarkerHostNames.BeforeMarker), after = Term.A(MailboxReceiveMarkerHostNames.AfterMarker), priority = Term.A(MailboxReceiveMarkerHostNames.PriorityMessage);
        box.Send(before);
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm id = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            id
        );
        box.Send(after);
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, id);
        Invoke(box, MailboxReceiveMarkerHostNames.ClearReceiveMarker, id);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<Term?> Select(Term value)
        {
            entered.TrySetResult();
            await release.Task;

            return value;
        }
        MethodInfo method = typeof(Mailbox).GetMethod(MailboxReceiveMarkerHostNames.ReceiveSelectingAsync, Hidden)!.MakeGenericMethod(typeof(Term));
        var pending = (ValueTask<Term?>)method.Invoke(box, [
            (Func<Term, ValueTask<Term?>>)Select, TimeSpan.Zero, CancellationToken.None, null
        ])!;
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        box.Send(priority, true);
        release.TrySetResult();
        try
        {
            await pending;
            throw new InvalidOperationException(MailboxReceiveMarkerHostNames.PriorityMarkerInteractionWasSilentlyAccepted);
        }
        catch (NotSupportedException)
        {
        }
        Require(box.Count == 3 && Snapshot(box).SequenceEqual(new[] { priority, before, after }));
    });

    public static Task PriorityRefusalAfterExecutionWait() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term before = Term.A(MailboxReceiveMarkerHostNames.BeforeMarker);
        Term priority = Term.A(MailboxReceiveMarkerHostNames.PriorityMessage);
        box.Send(before);
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm id = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            id
        );
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, id);
        Invoke(box, MailboxReceiveMarkerHostNames.ClearReceiveMarker, id);
        PropertyInfo hook = typeof(Mailbox).GetProperty(MailboxReceiveMarkerHostNames.WaitForExecution, Hidden)!;
        object? original = hook.GetValue(box);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;

        ValueTask<long> Wait() => ++calls == 1 ? ValueTask.FromResult(0L) : Hold();

        async ValueTask<long> Hold()
        {
            entered.TrySetResult();

            return await release.Task;
        }
        hook.SetValue(box, (Func<ValueTask<long>>)Wait);
        Task<Term?> pending = box.ReceiveAsync<Term>(value => value, TimeSpan.FromMilliseconds(50)).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            box.Send(priority, true);
            release.TrySetResult(0);
            bool refused = false;
            try
            {
                await pending;
            }
            catch (NotSupportedException)
            {
                refused = true;
            }
            Require(refused && box.Count == 2 && Snapshot(box).SequenceEqual(new[] { priority, before }));
        }
        finally
        {
            release.TrySetResult(0);
            hook.SetValue(box, original);
        }
    });

    public static Task DeferredPhysicalOrder() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term deferred = Term.A(MailboxReceiveMarkerHostNames.DeferredBeforeMarker), after = Term.A(MailboxReceiveMarkerHostNames.AfterMarker);
        int releases = 0;
        AddDeferred(
            box,
            deferred,
            false,
            () => releases++
        );
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm id = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            id
        );
        CompleteDeferred(box);
        box.Send(after);
        Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, id);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(after));
        Require(releases == 1 && box.Count == 1);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(deferred));
    });

    public static Task DeferredPriorityRefused() => InActor(async (runtime, context) =>
    {
        Mailbox box = context.Mailbox;
        Term priority = Term.A(MailboxReceiveMarkerHostNames.DeferredPriority);
        int releases = 0;
        AddDeferred(
            box,
            priority,
            true,
            () => releases++
        );
        object token = Invoke(box, MailboxReceiveMarkerHostNames.ReserveReceiveMarker)!;
        ReferenceTerm id = runtime.MakeReference();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.BindReceiveMarker,
            token,
            id
        );
        CompleteDeferred(box);
        Reject(() => Invoke(box, MailboxReceiveMarkerHostNames.UseReceiveMarker, id));
        Require(releases == 1 && box.Count == 1);
        Require((await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero))!.Equals(priority));
    });

    private static void AddDeferred(
        Mailbox box,
        Term message,
        bool priority,
        Action release
    )
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(MailboxReceiveMarkerHostNames.ErlangDeferredMailboxAdmission, true)!;
        object value = Activator.CreateInstance(type, message, priority)!;
        Type signature = typeof(Func<>).MakeGenericType(type);
        Delegate prepare = Expression.Lambda(signature, Expression.Constant(value, type)).Compile();
        Invoke(
            box,
            MailboxReceiveMarkerHostNames.SendDeferred,
            prepare,
            release,
            null,
            null
        );
    }

    private static void CompleteDeferred(Mailbox box)
    {
        object claim = Invoke(box, MailboxReceiveMarkerHostNames.ClaimDeferred)!;
        Delegate prepare = (Delegate)claim.GetType().GetProperty(MailboxReceiveMarkerHostNames.Prepare)!.GetValue(claim)!;
        Delegate complete = (Delegate)claim.GetType().GetProperty(MailboxReceiveMarkerHostNames.Complete)!.GetValue(claim)!;
        complete.DynamicInvoke(prepare.DynamicInvoke());
    }

    private static IReadOnlyList<Term> Snapshot(Mailbox box) => (IReadOnlyList<Term>)Invoke(box, MailboxReceiveMarkerHostNames.Snapshot)!;

    private static int MarkerCount(Mailbox box)
    {
        object list = typeof(Mailbox).GetField(MailboxReceiveMarkerHostNames.ReceiveMarkers, Hidden)!.GetValue(box)!;

        return (int)list.GetType().GetProperty(MailboxReceiveMarkerHostNames.Count)!.GetValue(list)!;
    }

    private static object? Invoke(Mailbox box, string method, params object?[] arguments)
    {
        try
        {
            return typeof(Mailbox).GetMethod(method, Hidden)!.Invoke(box, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (NotSupportedException)
        {
            return;
        }
        throw new InvalidOperationException(MailboxReceiveMarkerHostNames.ExpectedScopedPriorityMarkerRefusal);
    }

    private static async Task InActor(Func<ProcessRuntime, ProcessContext, Task> action)
    {
        await using var runtime = new ProcessRuntime();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                await action(runtime, context);
                completed.TrySetResult();
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(20));
        Require((await actor.Completion.WaitAsync(TimeSpan.FromSeconds(10))).Equals(Term.A(MailboxReceiveMarkerHostNames.Normal)));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(MailboxReceiveMarkerHostNames.MailboxReceiveMarkerContractFailed);
    }
}
