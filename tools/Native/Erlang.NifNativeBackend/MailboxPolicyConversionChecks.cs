using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

public static class MailboxPolicyConversionChecks
{
    public static Task OnToOff() => Run(true);

    public static Task OffToOn() => Run(false);

    private static async Task Run(bool toOffHeap)
    {
        foreach (bool owned in new[] { false, true }) await Run(toOffHeap, owned);
    }

    private static async Task Run(bool toOffHeap, bool owned)
    {
        await using var runtime = NativeOwned516Profile.Create(owned);
        var initial = toOffHeap ? ProcessMessageQueueDataMode411.OnHeap : ProcessMessageQueueDataMode411.OffHeap;
        var target = toOffHeap ? ProcessMessageQueueDataMode411.OffHeap : ProcessMessageQueueDataMode411.OnHeap;
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var change = Signal();
        var changed = Signal();
        var collect = Signal();
        var collected = Signal();
        var receive = Signal();
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                context.Mailbox.ExchangeMessageQueueData411(initial);
                ready.TrySetResult(context);
                await change.Task.WaitAsync(context.Cancellation);
                Term old = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangModuleNames.ProcessFlag,
                    Term.A(ProcessMessageQueueDataNames411.Flag),
                    ProcessMessageQueueData411.Atom(target)
                );
                CpuStatisticsAccess.Require(old.Equals(ProcessMessageQueueData411.Atom(initial)));
                _ = context.Mailbox.Count; // Real owner signal boundary, not manual conversion.
                if (toOffHeap)
                {
                    // OTP returns the flag before its thread-progress/move
                    // signal completes. Wait for ACTUAL storage transition,
                    // never assume one Count call acknowledged that move.
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation);
                    deadline.CancelAfter(MailboxBackingCaseNames.Deadline);
                    while (QueueOwnedByProcess(context))
                    {
                        await Task.Delay(MailboxPolicyConversionCaseNames.Poll, deadline.Token);
                        _ = context.Mailbox.Count;
                    }
                }
                changed.TrySetResult();
                await collect.Task.WaitAsync(context.Cancellation);
                _ = context.CollectOwnedGeneration508(true);
                collected.TrySetResult();
                await receive.Task.WaitAsync(context.Cancellation);
                _ = await context.Mailbox.ReceiveAsync<Term>(value => value,
                    MailboxBackingCaseNames.Deadline, context.Cancellation)
                    ?? throw new InvalidOperationException();
            }
            catch (Exception error)
            {
                ready.TrySetException(error);
                changed.TrySetException(error);
                collected.TrySetException(error);
                throw;
            }

            return Nil.Value;
        });
        Exception? failure = null;
        try
        {
            ProcessContext context = await ready.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            Term value = Term.Tuple(Term.A(MailboxBackingCaseNames.Tag), Term.List(Term.I(5), Term.I(7)));
            runtime.Send(actor.Pid, value);
            var before = MailboxBackingProbe.ReadSingle(context.Mailbox);
            var initialStorage = MailboxBackingProbe.Storage(before.Value)
                ?? throw new InvalidOperationException();
            CpuStatisticsAccess.Require(ReferenceEquals(initialStorage.Owner, context.TermHeap506) == toOffHeap);
            change.TrySetResult();
            await changed.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            var middle = MailboxBackingProbe.ReadSingle(context.Mailbox);
            var middleStorage = MailboxBackingProbe.Storage(middle.Value)
                ?? throw new InvalidOperationException();
            // Off->on does NOT eagerly copy old fragments at process_flag.
            CpuStatisticsAccess.Require(!ReferenceEquals(middleStorage.Owner, context.TermHeap506));
            CpuStatisticsAccess.Require(middle.Value.Equals(value));
            collect.TrySetResult();
            await collected.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            var after = MailboxBackingProbe.ReadSingle(context.Mailbox);
            var finalStorage = MailboxBackingProbe.Storage(after.Value)
                ?? throw new InvalidOperationException();
            CpuStatisticsAccess.Require(ReferenceEquals(finalStorage.Owner, context.TermHeap506) != toOffHeap);
            CpuStatisticsAccess.Require(after.Value.Equals(value));
            await before.Work!.Completion.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            CpuStatisticsAccess.Require(!ReferenceEquals(before.Work, after.Work));
            Term snapshot = context.Mailbox.Snapshot().Single();
            CpuStatisticsAccess.Require(snapshot.Equals(value) && MailboxBackingProbe.Storage(snapshot) is null);
            receive.TrySetResult();
            await after.Work!.Completion.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            GC.KeepAlive(before.Value);
            GC.KeepAlive(middle.Value);
            GC.KeepAlive(after.Value);
            GC.KeepAlive(snapshot);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            change.TrySetResult();
            collect.TrySetResult();
            receive.TrySetResult();
        }
        try
        {
            CpuStatisticsAccess.Require((await actor.Completion.WaitAsync(MailboxBackingCaseNames.Deadline))
                .Equals(Term.A(ProcessExitReasons.Normal)));
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static bool QueueOwnedByProcess(ProcessContext context)
    {
        object gate = typeof(Mailbox).GetField(MailboxBackingCaseNames.GateField,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(context.Mailbox)!;
        lock (gate)
        {
            var actual = MailboxBackingProbe.ReadSingle(context.Mailbox);
            var storage = MailboxBackingProbe.Storage(actual.Value)
                ?? throw new InvalidOperationException();

            return ReferenceEquals(storage.Owner, context.TermHeap506);
        }
    }

    private static TaskCompletionSource Signal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
