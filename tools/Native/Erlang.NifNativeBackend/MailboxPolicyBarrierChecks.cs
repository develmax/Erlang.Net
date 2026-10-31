using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

public static class MailboxPolicyBarrierChecks
{
    public static async Task Run()
    {
        foreach (bool owned in new[] { false, true }) await Run(owned);
    }

    private static async Task Run(bool owned)
    {
        await using var runtime = NativeOwned516Profile.Create(owned);
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);
            await start.Task.WaitAsync(context.Cancellation);
            Term old = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.ProcessFlag,
                Term.A(ProcessMessageQueueDataNames411.Flag),
                ProcessMessageQueueData411.Atom(ProcessMessageQueueDataMode411.OffHeap)
            );
            Require(old.Equals(ProcessMessageQueueData411.Atom(ProcessMessageQueueDataMode411.OnHeap)));
            bool laterAdmitted = false;
            context.Mailbox.SendPendingSnapshot593(
                Nil.Value,
                () =>
 {
     laterAdmitted = true;

     return null;
 },
                traceAdmission: false
            );
            _ = context.Mailbox.Count;
            Require(!laterAdmitted && context.Mailbox.HasMessageQueuePolicyWork);
            var partial = Values(context.Mailbox);
            Require(partial.Length == MailboxPolicyBarrierCaseNames.Messages);
            Require(partial.Count(value => ReferenceEquals(MailboxBackingProbe.Storage(value)!.Owner, context.TermHeap506))
                == MailboxPolicyBarrierCaseNames.Messages - MessageQueuePolicyParameters.EntriesPerBatch);
            _ = context.Mailbox.Count;
            Require(!laterAdmitted && !context.Mailbox.HasMessageQueuePolicyWork);
            Require(Values(context.Mailbox).All(value => !ReferenceEquals(MailboxBackingProbe.Storage(value)!.Owner, context.TermHeap506)));
            _ = context.Mailbox.Count;
            Require(laterAdmitted);
            for (int index = 0; index < MailboxPolicyBarrierCaseNames.Messages; index++)
            {
                Term? value = await context.Mailbox.ReceiveAsync<Term>(item => item, MailboxBackingCaseNames.Deadline, context.Cancellation);
                Require(value is TupleTerm tuple && tuple.Items[1].Equals(Term.I(index)));
            }

            return Nil.Value;
        });
        Exception? failure = null;
        try
        {
            await ready.Task.WaitAsync(MailboxBackingCaseNames.Deadline);
            for (int index = 0; index < MailboxPolicyBarrierCaseNames.Messages; index++)
                runtime.Send(actor.Pid, Term.Tuple(Term.A(MailboxBackingCaseNames.Tag), Term.I(index)));
            start.TrySetResult();
            Require((await actor.Completion.WaitAsync(MailboxBackingCaseNames.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            start.TrySetResult();
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    // Read actual queue values; no private mutation, policy setters, or GC credit.
    private static Term[] Values(Mailbox mailbox)
    {
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = typeof(Mailbox).GetField(MailboxBackingCaseNames.GateField, members)!.GetValue(mailbox)!;
        lock (gate)
        {
            var values = new List<Term>();
            var entries = (IEnumerable)typeof(Mailbox).GetField(MailboxBackingCaseNames.MessagesField, members)!.GetValue(mailbox)!;
            foreach (object entry in entries)
            {
                Type type = entry.GetType();
                if ((bool)type.GetProperty(MailboxPolicyConversionCaseNames.ControlProperty)!.GetValue(entry)!
                    || type.GetProperty(MailboxPolicyConversionCaseNames.AdmissionProperty)!.GetValue(entry) is not null) continue;
                values.Add((Term)type.GetProperty(MailboxBackingCaseNames.ValueProperty)!.GetValue(entry)!);
            }

            return values.ToArray();
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(MessageQueuePolicyParameters.Invalid);
    }
}
