using System.Diagnostics;
using System.Reflection;
using Erlang;

namespace Erlang.Tests;

public static class MailboxSelectionChecks
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(MailboxSelectionCheckNames.SafetyDeadlineSeconds);

    public static async Task Run()
    {
        await SelectorOutsideGate();
        await PredicateOutsideGate(false);
        await PredicateOutsideGate(true);
        await PriorityInvalidatesPrecedence();
        await AppendedTailIsScanned();
        await RemovedEqualNodeIsNotConsumed();
        await CancellationBeforeCommit();
        await HaltBeforeCommit();
        FifoAllocationGrowth();
        await BoundedAppendBatch();
        await FinitePriorityRetries();
        await FiniteOrdinaryProducer();
    }

    private static void FifoAllocationGrowth()
    {
        _ = ConsumeFifo(MailboxSelectionCheckNames.WarmupMessages);
        long small = ConsumeFifo(MailboxSelectionCheckNames.SmallFifoMessages);
        long large = ConsumeFifo(MailboxSelectionCheckNames.LargeFifoMessages);
        Require(large <= small * MailboxSelectionCheckNames.LinearAllocationAllowance + MailboxSelectionCheckNames.FixedAllocationAllowanceBytes);
    }

    private static long ConsumeFifo(int count)
    {
        var mailbox = new Mailbox();
        for (int index = 0; index < count; index++)
            mailbox.Send(Term.I(index));
        int calls = 0;

        Term? Select(Term value)
        {
            calls++;

            return value;
        }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < count; index++)
        {
            ValueTask<Term?> pending = mailbox.ReceiveAsync(Select, TimeSpan.Zero);
            Require(pending.IsCompletedSuccessfully && pending.Result!.Equals(Term.I(index)));
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(calls == count && mailbox.Count == 0);

        return allocated;
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task SelectorOutsideGate()
    {
        var mailbox = new Mailbox();
        Term seed = Term.A(MailboxSelectionCheckNames.Seed);
        Term tail = Term.A(MailboxSelectionCheckNames.Tail);
        mailbox.Send(seed);
        var entered = Signal();
        var release = Signal();
        int calls = 0;
        Task<Term?> receive = Task.Run(async () => await mailbox.ReceiveAsync(
            value =>
        {
            if (Interlocked.Increment(ref calls) == MailboxSelectionCheckNames.SingleMessage)
            {
                entered.TrySetResult();
                release.Task.GetAwaiter().GetResult();
            }

            return value;
        },
            TimeSpan.Zero
        ));
        await WhileBlocked(
            entered,
            release,
            receive,
            () => Task.Run(() => mailbox.Send(tail))
        );
        Require((await receive)!.Equals(seed) && calls == MailboxSelectionCheckNames.SingleMessage);
        Require((await mailbox.ReceiveAsync(value => value, TimeSpan.Zero))!.Equals(tail));
    }

    private static async Task PredicateOutsideGate(bool bulk)
    {
        var mailbox = new Mailbox();
        Term seed = Term.A(MailboxSelectionCheckNames.Seed);
        Term tail = Term.A(MailboxSelectionCheckNames.Tail);
        mailbox.Send(seed);
        var entered = Signal();
        var release = Signal();
        int calls = 0;

        bool Predicate(Term value)
        {
            if (Interlocked.Increment(ref calls) == MailboxSelectionCheckNames.SingleMessage)
            {
                entered.TrySetResult();
                release.Task.GetAwaiter().GetResult();
            }

            return value.Equals(seed);
        }
        Task<int> removal = Task.Run(() => bulk ? mailbox.Remove(Predicate) : mailbox.RemoveFirst(Predicate) ? MailboxSelectionCheckNames.SingleMessage : 0);
        await WhileBlocked(
            entered,
            release,
            removal,
            () => Task.Run(() => mailbox.Send(tail))
        );
        Require(await removal == MailboxSelectionCheckNames.SingleMessage);
        Require(mailbox.Count == MailboxSelectionCheckNames.SingleMessage);
        Require((await mailbox.ReceiveAsync(value => value, TimeSpan.Zero))!.Equals(tail));
    }

    private static async Task PriorityInvalidatesPrecedence()
    {
        var mailbox = new Mailbox();
        Term seed = Term.A(MailboxSelectionCheckNames.Seed);
        Term urgent = Term.A(MailboxSelectionCheckNames.Urgent);
        mailbox.Send(seed);
        int inserted = 0;
        Term? selected = await mailbox.ReceiveAsync(value =>
        {
            if (Interlocked.CompareExchange(ref inserted, 1, 0) == 0)
                mailbox.Send(urgent, true);

            return value;
        }, TimeSpan.Zero);
        Require(selected!.Equals(urgent));
        Require((await mailbox.ReceiveAsync(value => value, TimeSpan.Zero))!.Equals(seed));
    }

    private static async Task AppendedTailIsScanned()
    {
        var mailbox = new Mailbox();
        Term seed = Term.A(MailboxSelectionCheckNames.Unmatched);
        Term wanted = Term.A(MailboxSelectionCheckNames.Tail);
        mailbox.Send(seed);
        var entered = Signal();
        var release = Signal();
        int calls = 0;
        Task<Term?> receive = Task.Run(async () => await mailbox.ReceiveAsync(
            value =>
        {
            if (Interlocked.Increment(ref calls) == MailboxSelectionCheckNames.SingleMessage)
            {
                entered.TrySetResult();
                release.Task.GetAwaiter().GetResult();
            }

            return value.Equals(wanted) ? value : null;
        },
            Deadline
        ));
        await WhileBlocked(
            entered,
            release,
            receive,
            () => Task.Run(() => mailbox.Send(wanted))
        );
        Require((await receive)!.Equals(wanted) && mailbox.Count == MailboxSelectionCheckNames.SingleMessage
            && calls == MailboxSelectionCheckNames.RevalidatedSelectors);
    }

    private static async Task RemovedEqualNodeIsNotConsumed()
    {
        var mailbox = new Mailbox();
        Term same = Term.A(MailboxSelectionCheckNames.Same);
        mailbox.Send(same);
        mailbox.Send(same);
        var entered = Signal();
        var release = Signal();
        int calls = 0;
        Task<Term?> receive = Task.Run(async () => await mailbox.ReceiveAsync(
            value =>
        {
            if (Interlocked.Increment(ref calls) == MailboxSelectionCheckNames.SingleMessage)
            {
                entered.TrySetResult();
                release.Task.GetAwaiter().GetResult();
            }

            return value;
        },
            TimeSpan.Zero
        ));
        await WhileBlocked(
            entered,
            release,
            receive,
            () => Task.Run(() => Require(mailbox.RemoveFirst(value => value.Equals(same))))
        );
        Require((await receive)!.Equals(same) && calls == MailboxSelectionCheckNames.RevalidatedSelectors && mailbox.Count == 0);
    }

    private static async Task CancellationBeforeCommit()
    {
        var mailbox = new Mailbox();
        mailbox.Send(Term.A(MailboxSelectionCheckNames.Seed));
        using var cancellation = new CancellationTokenSource();
        var entered = Signal();
        var release = Signal();
        Task<Term?> receive = Task.Run(async () => await mailbox.ReceiveAsync(value =>
        {
            entered.TrySetResult();
            release.Task.GetAwaiter().GetResult();

            return value;
        }, TimeSpan.Zero, cancellation.Token));
        await WhileBlocked(
            entered,
            release,
            receive,
            () => Task.Run(cancellation.Cancel)
        );
        try
        {
            _ = await receive;
            throw new InvalidOperationException(MailboxSelectionCheckNames.Failed);
        }
        catch (OperationCanceledException)
        {
            Require(mailbox.Count == MailboxSelectionCheckNames.SingleMessage);
        }
    }

    private static async Task HaltBeforeCommit()
    {
        await using var runtime = new ProcessRuntime();
        var entered = Signal();
        var release = Signal();
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessContext? actualContext = null;
        ProcessHandle owner = runtime.Spawn(async context =>
        {
            actualContext = context;
            _ = await runtime.Modules.Call(
                context,
                MailboxSelectionCheckNames.Erlang,
                MailboxSelectionCheckNames.ProcessFlag,
                Term.A(MailboxSelectionCheckNames.SaveCalls),
                Term.I(MailboxSelectionCheckNames.SavedCallCapacity)
            );
            context.Mailbox.Send(Term.A(MailboxSelectionCheckNames.Seed));
            try
            {
                _ = await context.ReceiveAsync(value =>
                {
                    entered.TrySetResult();
                    release.Task.GetAwaiter().GetResult();

                    return value;
                }, TimeSpan.Zero);
                cancelled.TrySetResult(false);
            }
            catch (ProcessHaltTransfer transfer) when (ReferenceEquals(transfer.Runtime, runtime))
            {
                cancelled.TrySetResult(true);
            }

            return Term.A(MailboxSelectionCheckNames.Completed);
        });
        await WhileBlocked(
            entered,
            release,
            owner.Completion,
            () => Task.Run(() => runtime.RequestImmediateHalt(MailboxSelectionCheckNames.HaltStatus))
        );
        _ = await owner.Completion;
        await runtime.HaltQuiescence.WaitAsync(Deadline);
        Require(cancelled.Task.IsCompletedSuccessfully && cancelled.Task.Result);
        object calls = typeof(ProcessContext).GetProperty(MailboxSelectionCheckNames.SavedCallsProperty,
            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(actualContext)!;
        Term history = (Term)calls.GetType().GetMethod(MailboxSelectionCheckNames.SnapshotMethod)!.Invoke(calls, null)!;
        Require(!Cons.Items(history).Any(value => value.Equals(Term.A(MailboxSelectionCheckNames.ReceiveMarker))));
    }

    private static async Task BoundedAppendBatch()
    {
        var mailbox = new Mailbox();
        mailbox.Send(Term.A(MailboxSelectionCheckNames.Unmatched));
        int calls = 0;
        Term? result = await mailbox.ReceiveAsync<Term>(
            value =>
        {
            calls++;
            if (calls < MailboxSelectionCheckNames.BoundedProducerMaximum)
                mailbox.Send(Term.A(MailboxSelectionCheckNames.Unmatched));

            return null;
        },
            TimeSpan.Zero
        );
        Require(result is null && calls == MailboxSelectionCheckNames.SingleMessage);
        Require(mailbox.Count == MailboxSelectionCheckNames.RevalidatedSelectors);
    }

    private static async Task FinitePriorityRetries()
    {
        var mailbox = new Mailbox();
        mailbox.Send(Term.A(MailboxSelectionCheckNames.Seed));
        int calls = 0;
        Term? result = await mailbox.ReceiveAsync<Term>(
            value =>
        {
            calls++;
            if (calls < MailboxSelectionCheckNames.BoundedProducerMaximum)
                mailbox.Send(Term.A(MailboxSelectionCheckNames.Urgent), true);

            return null;
        },
            TimeSpan.FromMilliseconds(MailboxSelectionCheckNames.FiniteTimeoutMilliseconds)
        );
        Require(result is null && calls > 0);
    }

    private static async Task FiniteOrdinaryProducer()
    {
        var mailbox = new Mailbox();
        mailbox.Send(Term.A(MailboxSelectionCheckNames.Unmatched));
        var watch = Stopwatch.StartNew();
        bool watchdogReached = false;
        int calls = 0;
        Term? result = await mailbox.ReceiveAsync<Term>(
            value =>
        {
            long selectedAt = Stopwatch.GetTimestamp();
            while (Stopwatch.GetElapsedTime(selectedAt) < TimeSpan.FromMilliseconds(MailboxSelectionCheckNames.SelectorWorkMilliseconds))
                Thread.SpinWait(MailboxSelectionCheckNames.SelectorSpinIterations);
            calls++;
            if (watch.Elapsed >= TimeSpan.FromMilliseconds(MailboxSelectionCheckNames.ProducerWatchdogMilliseconds))
                watchdogReached = true;
            else
                mailbox.Send(Term.A(MailboxSelectionCheckNames.Unmatched));

            return null;
        },
            TimeSpan.FromMilliseconds(MailboxSelectionCheckNames.FiniteTimeoutMilliseconds)
        );
        Require(result is null && !watchdogReached && calls > 0);
    }

    private static async Task WhileBlocked(
        TaskCompletionSource entered,
        TaskCompletionSource release,
        Task terminal,
        Func<Task> concurrent
    )
    {
        Exception? failure = null;
        Task action = Task.CompletedTask;
        try
        {
            await entered.Task.WaitAsync(Deadline);
            action = concurrent();
            await action.WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            release.TrySetResult();
            try
            {
                await Task.WhenAll(
                    terminal.ContinueWith(_ => { }, TaskScheduler.Default),
                    action.ContinueWith(_ => { }, TaskScheduler.Default)
                ).WaitAsync(Deadline);
            }
            catch (Exception cleanupError)
            {
                failure ??= cleanupError;
            }
        }
        if (failure is not null)
            throw new InvalidOperationException(MailboxSelectionCheckNames.Failed, failure);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(MailboxSelectionCheckNames.Failed);
    }
}
