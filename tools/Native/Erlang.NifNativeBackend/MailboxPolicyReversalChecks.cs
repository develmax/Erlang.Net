using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

public static class MailboxPolicyReversalChecks
{
    public static async Task Run()
    {
        foreach (bool owned in new[] { false, true }) await Run(owned);
    }

    private static async Task Run(bool owned)
    {
        await using var runtime = NativeOwned516Profile.Create(owned);
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reverse = Signal();
        var reversed = Signal();
        var offAgain = Signal();
        var offMoved = Signal();
        var onAgain = Signal();
        var onRequested = Signal();
        var gc = Signal();
        var collected = Signal();
        var consume = Signal();
        Term first = Term.Tuple(Term.A(MailboxBackingCaseNames.Tag), Term.List(Term.I(5), Term.I(7)));
        Term second = Term.Tuple(Term.A(MailboxBackingCaseNames.Tag), Term.List(Term.I(11), Term.I(13)));
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                ready.TrySetResult(context);
                await reverse.Task.WaitAsync(context.Cancellation);
                await Set(context, ProcessMessageQueueDataMode411.OffHeap, ProcessMessageQueueDataMode411.OnHeap);
                await Set(context, ProcessMessageQueueDataMode411.OnHeap, ProcessMessageQueueDataMode411.OffHeap);
                reversed.TrySetResult();
                await offAgain.Task.WaitAsync(context.Cancellation);
                await Set(context, ProcessMessageQueueDataMode411.OffHeap, ProcessMessageQueueDataMode411.OnHeap);
                _ = context.Mailbox.Count; // REAL control admission/move, no private mutation.
                offMoved.TrySetResult();
                await onAgain.Task.WaitAsync(context.Cancellation);
                await Set(context, ProcessMessageQueueDataMode411.OnHeap, ProcessMessageQueueDataMode411.OffHeap);
                onRequested.TrySetResult();
                await gc.Task.WaitAsync(context.Cancellation);
                _ = context.CollectOwnedGeneration508(true);
                collected.TrySetResult();
                await consume.Task.WaitAsync(context.Cancellation);
                foreach (Term expected in new[] { first, second })
                {
                    Term? value = await context.Mailbox.ReceiveAsync<Term>(term => term, MailboxBackingCaseNames.Deadline, context.Cancellation);
                    Require(value is not null && value.Equals(expected));
                }
            }
            catch (Exception error)
            {
                ready.TrySetException(error);
                reversed.TrySetException(error);
                offMoved.TrySetException(error);
                onRequested.TrySetException(error);
                collected.TrySetException(error);
                throw;
            }

            return Nil.Value;
        });
        Exception? failure = null;
        try
        {
            ProcessContext context = await ready.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            runtime.Send(actor.Pid, first);
            var initial = Read(context.Mailbox).Single();
            Require(ReferenceEquals(MailboxBackingProbe.Storage(initial.Value)!.Owner, context.TermHeap506));
            reverse.TrySetResult();
            await reversed.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            Require(context.Mailbox.MessageQueueData411 == ProcessMessageQueueDataMode411.OnHeap
                && context.Mailbox.MessageQueuePlacementMode == ProcessMessageQueueDataMode411.OffHeap);
            runtime.Send(actor.Pid, second);
            var mixed = Read(context.Mailbox);
            Require(mixed.Length == 2 && mixed[0].Value.Equals(first) && mixed[1].Value.Equals(second)
                && ReferenceEquals(MailboxBackingProbe.Storage(mixed[0].Value)!.Owner, context.TermHeap506)
                && !ReferenceEquals(MailboxBackingProbe.Storage(mixed[1].Value)!.Owner, context.TermHeap506));
            offAgain.TrySetResult();
            await offMoved.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            var off = Read(context.Mailbox);
            Require(off.Length == 2 && off.All(item => !ReferenceEquals(MailboxBackingProbe.Storage(item.Value)!.Owner, context.TermHeap506)));
            await initial.Work.Completion.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            onAgain.TrySetResult();
            await onRequested.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            Require(Read(context.Mailbox).All(item => !ReferenceEquals(MailboxBackingProbe.Storage(item.Value)!.Owner, context.TermHeap506)));
            gc.TrySetResult();
            await collected.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            var on = Read(context.Mailbox);
            Require(on.Length == 2 && on[0].Value.Equals(first) && on[1].Value.Equals(second)
                && on.All(item => ReferenceEquals(MailboxBackingProbe.Storage(item.Value)!.Owner, context.TermHeap506)));
            foreach (var item in off) await item.Work.Completion.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            consume.TrySetResult();
            foreach (var item in on) await item.Work.Completion.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            GC.KeepAlive(initial.Value);
            GC.KeepAlive(mixed);
            GC.KeepAlive(off);
            GC.KeepAlive(on);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            reverse.TrySetResult();
            offAgain.TrySetResult();
            onAgain.TrySetResult();
            gc.TrySetResult();
            consume.TrySetResult();
        }
        try
        {
            Require((await actor.Completion.WaitAsync(MailboxBackingCaseNames.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();

        async Task Set(ProcessContext context, ProcessMessageQueueDataMode411 mode, ProcessMessageQueueDataMode411 expected)
        {
            Term old = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.ProcessFlag,
                Term.A(ProcessMessageQueueDataNames411.Flag),
                ProcessMessageQueueData411.Atom(mode)
            );
            Require(old.Equals(ProcessMessageQueueData411.Atom(expected)));
        }
    }

    private static (Term Value, MailboxPayloadLease593 Work)[] Read(Mailbox mailbox)
    {
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = typeof(Mailbox).GetField(MailboxBackingCaseNames.GateField, members)!.GetValue(mailbox)!;
        lock (gate)
        {
            var result = new List<(Term, MailboxPayloadLease593)>();
            var entries = (IEnumerable)typeof(Mailbox).GetField(MailboxBackingCaseNames.MessagesField, members)!.GetValue(mailbox)!;
            foreach (object entry in entries)
            {
                Type type = entry.GetType();
                if ((bool)type.GetProperty(MailboxPolicyConversionCaseNames.ControlProperty)!.GetValue(entry)! || type.GetProperty(MailboxPolicyConversionCaseNames.AdmissionProperty)!.GetValue(entry) is not null) continue;
                Term value = (Term)type.GetProperty(MailboxBackingCaseNames.ValueProperty)!.GetValue(entry)!;
                var custody = (MailboxEntryCustody593?)type.GetProperty(MailboxBackingCaseNames.CustodyProperty)!.GetValue(entry);
                Require(custody is not null);
                var work = (MailboxPayloadLease593)typeof(MailboxEntryCustody593)
                    .GetField(MailboxBackingCaseNames.WorkField, members)!.GetValue(custody)!;
                result.Add((value, work));
            }

            return result.ToArray();
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(MessageQueuePolicyParameters.Invalid);
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
