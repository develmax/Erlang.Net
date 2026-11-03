using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

public static class MailboxBackingChecks
{
    public static Task OnHeap() => Run(ProcessMessageQueueDataMode411.OnHeap);

    public static Task OffHeap() => Run(ProcessMessageQueueDataMode411.OffHeap);

    private static async Task Run(ProcessMessageQueueDataMode411 mode)
    {
        await using var runtime = new ProcessRuntime(2);
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var collect = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var collected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receive = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var final = new TaskCompletionSource<ProcessTermCollection508>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                context.Mailbox.ExchangeMessageQueueData411(mode);
                ready.TrySetResult(context);
                await collect.Task.WaitAsync(context.Cancellation);
                _ = context.CollectOwnedGeneration508(true);
                collected.TrySetResult();
                await receive.Task.WaitAsync(context.Cancellation);
                Term value = await context.Mailbox.ReceiveAsync<Term>(item => item,
                    MailboxBackingCaseNames.Deadline, context.Cancellation)
                    ?? throw new InvalidOperationException();
                received.TrySetResult(value);
                await finish.Task.WaitAsync(context.Cancellation);
                final.TrySetResult(context.CollectOwnedGeneration508(true));
            }
            catch (Exception error)
            {
                ready.TrySetException(error);
                collected.TrySetException(error);
                received.TrySetException(error);
                final.TrySetException(error);
                throw;
            }

            return Nil.Value;
        });
        Exception? failure = null;
        try
        {
            ProcessContext context = await ready.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            Term source = Term.Tuple(Term.A(MailboxBackingCaseNames.Tag),
                Term.List(Term.Tuple(Term.A(MailboxBackingCaseNames.Tail), Term.I(7))));
            CpuStatisticsAccess.Require(runtime.Send(actor.Pid, source).Equals(source));
            var actual = MailboxBackingProbe.ReadSingle(context.Mailbox);
            var storage = MailboxBackingProbe.Storage(actual.Value);
            CpuStatisticsAccess.Require(storage is not null);
            var actualHeap = (ProcessTermHeap506)storage!.Owner;
            CpuStatisticsAccess.Require(ReferenceEquals(actualHeap, context.TermHeap506)
                == (mode == ProcessMessageQueueDataMode411.OnHeap));
            CpuStatisticsAccess.Require(actualHeap.Snapshot.LivePayloadWords > 0);
            collect.TrySetResult();
            await collected.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            // Actual queue root retained this exact physical graph through GC.
            CpuStatisticsAccess.Require(actual.Value.Equals(source));
            Term snapshot = context.Mailbox.Snapshot().Single();
            CpuStatisticsAccess.Require(snapshot.Equals(source));
            CpuStatisticsAccess.Require(MailboxBackingProbe.Storage(snapshot) is null);
            receive.TrySetResult();
            Term escaped = await received.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            await actual.Work!.Completion.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            if (mode == ProcessMessageQueueDataMode411.OffHeap)
                CpuStatisticsAccess.Require(actualHeap.Snapshot.CapacityWords == 0
                    && actualHeap.Snapshot.RootFrames == 0);
            finish.TrySetResult();
            ProcessTermCollection508 reclaimed = await final.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            if (mode == ProcessMessageQueueDataMode411.OnHeap)
                CpuStatisticsAccess.Require(reclaimed.ReclaimedAllocationWords567 > 0);
            // Stale physical Value, snapshots and returned values all remain CLR
            // reachable. Only real queue/result lifetime determines backing.
            CpuStatisticsAccess.Require(snapshot.Equals(source) && escaped.Equals(source));
            CpuStatisticsAccess.Require(MailboxBackingProbe.Storage(escaped) is null);
            GC.KeepAlive(actual.Value);
            GC.KeepAlive(snapshot);
            GC.KeepAlive(escaped);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            collect.TrySetResult();
            receive.TrySetResult();
            finish.TrySetResult();
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
}
