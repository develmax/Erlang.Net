using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Linq.Expressions;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

using static Erlang.Tests.MailboxBeamCallback399Names;

namespace Erlang.Tests;

internal static class MailboxBeamCallback399Checks
{
    private static async ValueTask<Term?> Receive(
        Mailbox box,
        Func<Term, ValueTask<Term?>> select,
        Term? timeout = null,
        CancellationToken cancellation = default
    )
    {
        MethodInfo method = typeof(Mailbox).GetMethod(ReceiveMethod, Hidden)!.MakeGenericMethod(typeof(Term));
        try
        {
            return await (ValueTask<Term?>)method.Invoke(box, [select, timeout ?? Term.I(0), cancellation])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static async Task<Term[]> Drain(Mailbox box)
    {
        var values = new List<Term>();
        while (await box.ReceiveAsync<Term>(value => value, TimeSpan.Zero) is { } value)
            values.Add(value);

        return values.ToArray();
    }

    internal static async Task PriorityConsumesCurrentSave()
    {
        var box = new Mailbox();
        box.Send(A);
        box.Send(B);
        var visits = new List<Term>();
        Term? result = await Receive(
            box,
            value =>
        {
            Require(!(bool)Property(box, DeferredGateHeldProperty));
            visits.Add(value);
            box.Send(Priority, priority: true);

            return ValueTask.FromResult<Term?>(C);
        }
        );
        Require(result!.Equals(C) && visits.SequenceEqual(new[] { A }));
        Require((await Drain(box)).SequenceEqual(new[] { A, B }));
    }

    internal static async Task PriorityRejectionRepeatsSourceAttempt()
    {
        var box = new Mailbox();
        box.Send(A);
        box.Send(B);
        var visits = new List<Term>();
        try
        {
            await Receive(
                box,
                value =>
            {
                if (visits.Count == Attempts)
                    throw new ErlangException(BoundedError);
                visits.Add(value);
                box.Send(Priority, priority: true);

                return ValueTask.FromResult<Term?>(null);
            }
            );
            Require(false);
        }
        catch (ErlangException error)
        {
            Require(error.Reason.Equals(Term.A(BoundedError)));
        }
        Require(visits.SequenceEqual(Enumerable.Repeat(A, Attempts)));
        Require((await Drain(box)).SequenceEqual(new[] { A }.Concat(Enumerable.Repeat(Priority, Attempts)).Append(B)));
    }

    internal static async Task ExceptionAndNestedShareCursor()
    {
        var box = new Mailbox();
        box.Send(A);
        box.Send(B);
        box.Send(C);
        try
        {
            await Receive(
                box,
                value => value.Equals(A) ? ValueTask.FromResult<Term?>(null) : throw new ErlangException(CallbackError)
            );
            Require(false);
        }
        catch (ErlangException error)
        {
            Require(error.Reason.Equals(Term.A(CallbackError)));
        }
        Require((await Drain(box)).SequenceEqual(new[] { B, A, C }));
        box.Send(A);
        box.Send(B);
        box.Send(C);
        var visits = new List<Term>();
        Term? result = await Receive(
            box,
            async value =>
        {
            visits.Add(value);
            if (value.Equals(A))
            {
                Require((await box.ReceiveAsync<Term>(item => item.Equals(B) ? item : null, TimeSpan.Zero))!.Equals(B));

                return null;
            }

            return value.Equals(C) ? C : null;
        }
        );
        Require(result!.Equals(C) && visits.SequenceEqual(new[] { A, C }));
        Require((await Drain(box)).SequenceEqual(new[] { A }));
    }

    internal static async Task VisibilityCancellationAndReset()
    {
        var box = new Mailbox();
        box.Send(A);
        box.Send(B);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ValueTask<Term?> operation = Receive(
            box,
            async value =>
        {
            Require(!(bool)Property(box, DeferredGateHeldProperty) && box.Count == 2);
            entered.TrySetResult();
            await release.Task.WaitAsync(cancellation.Token);

            return value;
        },
            cancellation: cancellation.Token
        );
        await entered.Task.WaitAsync(CallbackDeadline);
        cancellation.Cancel();
        try
        {
            await operation;
            Require(false);
        }
        catch (OperationCanceledException)
        {
        }
        Require((await Drain(box)).SequenceEqual(new[] { A, B }));
        box.Send(A);
        try
        {
            await Receive(
                box,
                _ => ValueTask.FromResult<Term?>(null),
                Term.A(InvalidTimeoutAtom)
            );
            Require(false);
        }
        catch (ErlangException error)
        {
            Require(error.Reason.Equals(Term.A(ErlangErrorReasons.TimeoutValue)));
        }
        Require((await Drain(box)).SequenceEqual(new[] { A }));
        Call(box, StopMethod);
        Require(box.Count == 0);
    }

    internal static async Task NestedConsumesCurrentBeforeOuterCommit()
    {
        var box = new Mailbox();
        box.Send(A);
        box.Send(B);
        Term? result = await Receive(
            box,
            async value =>
        {
            Require((await box.ReceiveAsync<Term>(item => item.Equals(A) ? item : null, TimeSpan.Zero))!.Equals(A));

            return C;
        }
        );
        Require(result!.Equals(C) && box.Count == 0);
        box.Send(A);
        box.Send(B);
        box.Send(C);
        var visits = new List<Term>();
        result = await Receive(
            box,
            async value =>
        {
            visits.Add(value);
            if (value.Equals(A))
            {
                Require((await box.ReceiveAsync<Term>(item => item.Equals(A) ? item : null, TimeSpan.Zero))!.Equals(A));

                return null;
            }

            return value.Equals(C) ? C : null;
        }
        );
        Require(result!.Equals(C) && visits.SequenceEqual(new[] { A, C }));
        Require((await Drain(box)).SequenceEqual(new[] { B }));
    }

    internal static async Task MarkerReferencePriorityComposition()
    {
        await using var runtime = new ProcessRuntime();
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                Mailbox box = context.Mailbox;
                await Receive(box, _ => ValueTask.FromResult<Term?>(null));
                box.Send(A);
                object marker = Call(box, ReserveMarkerMethod);
                ReferenceTerm reference = runtime.MakeReference();
                Call(
                    box,
                    BindMarkerMethod,
                    marker,
                    reference
                );
                Call(
                    box,
                    UseMarkerMethod,
                    reference
                );
                box.Send(Priority, priority: true);
                box.Send(B);
                Require((await box.ReceiveAsync<Term>(item => item, TimeSpan.Zero))!.Equals(B));
                Require((await Drain(box)).SequenceEqual(new[] { Priority, A }));
                box.Send(A);
                marker = Call(box, ReserveMarkerMethod);
                reference = runtime.MakeReference();
                Call(
                    box,
                    BindMarkerMethod,
                    marker,
                    reference
                );
                Call(
                    box,
                    UseMarkerMethod,
                    reference
                );
                Term matching = Term.Tuple(reference, C);
                box.Send(matching, priority: true);
                Require((await box.ReceiveAsync<Term>(item => item, TimeSpan.Zero))!.Equals(matching));
                Require((await Drain(box)).SequenceEqual(new[] { A }));
                box.Send(A);
                marker = Call(box, ReserveMarkerMethod);
                reference = runtime.MakeReference();
                Call(
                    box,
                    BindMarkerMethod,
                    marker,
                    reference
                );
                matching = Term.Tuple(reference, C);
                box.Send(matching, priority: true);
                Call(
                    box,
                    UseMarkerMethod,
                    reference
                );
                Require((await box.ReceiveAsync<Term>(item => item, TimeSpan.Zero))!.Equals(matching));
                Require((await Drain(box)).SequenceEqual(new[] { A }));
                complete.TrySetResult();
            }
            catch (Exception error)
            {
                complete.TrySetException(error);
            }

            return Nil.Value;
        });
        await complete.Task.WaitAsync(ProcessDeadline);
        await process.Completion.WaitAsync(ProcessDeadline);
    }

    internal static async Task DeferredPriorityRetainsAdmissionOwnership()
    {
        var box = new Mailbox();
        box.Send(A);
        box.Send(B);
        int released = 0;
        Term? result = await Receive(
            box,
            value =>
        {
            Require(!(bool)Property(box, DeferredGateHeldProperty));
            System.Type type = typeof(ProcessRuntime).Assembly.GetType(DeferredAdmissionType, true)!;
            object admitted = Activator.CreateInstance(
                type,
                Priority,
                true
            )!;
            System.Type signature = typeof(Func<>).MakeGenericType(type);
            Delegate prepare = Expression.Lambda(
                signature,
                Expression.Constant(admitted, type)
            ).Compile();
            Call(
                box,
                SendDeferredMethod,
                prepare,
                (Action)(() => released++),
                null,
                null
            );
            object claim = Call(box, ClaimDeferredMethod);
            Delegate preparation = (Delegate)Property(claim, PrepareProperty);
            Delegate complete = (Delegate)Property(claim, CompleteProperty);
            complete.DynamicInvoke(preparation.DynamicInvoke());

            return ValueTask.FromResult<Term?>(C);
        }
        );
        Require(result!.Equals(C) && released == 1 && !(bool)Property(box, HasDeferredPendingProperty));
        Require((await Drain(box)).SequenceEqual(new[] { A, B }));
        Call(box, StopMethod);
        Require(released == 1 && box.Count == 0);
    }
}
