using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

public static class ReadonlyTracePrefix817Checks
{
    public static async Task ActualAliasProcessInfoAliasOrder()
    {
        foreach (bool owned in new[] { false, true })
            foreach (ProcessMessageQueueDataMode411 mode in new[] { ProcessMessageQueueDataMode411.OffHeap, ProcessMessageQueueDataMode411.OnHeap })
                await Exercise(owned, mode);
    }

    private static async Task Exercise(bool owned, ProcessMessageQueueDataMode411 mode)
    {
        await using var runtime = NativeOwned516Profile.Create(owned);
        await using var target = new ReadonlyTrace817Actor(runtime, ProcessMessageQueueDataMode411.OffHeap);
        await using var requester = new ReadonlyTrace817Actor(runtime, mode);
        ProcessContext targetContext = await target.Ready.WaitAsync(ReadonlyTracePrefix817CaseNames.Deadline);
        ProcessContext requesterContext = await requester.Ready.WaitAsync(ReadonlyTracePrefix817CaseNames.Deadline);
        ReferenceTerm alias = runtime.CreateAlias(targetContext, false, false);
        Term input = Term.Tuple(Term.A(ReadonlyTracePrefix817CaseNames.Payload), new BitString(new byte[ReadonlyTracePrefix817CaseNames.BinaryBytes]));
        runtime.Send(alias, input);
        Term? result = null;
        Task query = requester.InvokeAsync(async context =>
        {
            result = await runtime.QueryProcessInfo(
                context,
                targetContext.Self,
                [new ProcessInfoItem(Term.A(ProcessInfoNames.MessageQueueLength), ProcessInfoNames.MessageQueueLength)],
                true
            );
        });
        await Until(() => PendingInfo(runtime, targetContext));
        runtime.Send(alias, input);
        Mailbox.Entry[] sourceEntries = ReadEntries(targetContext.Mailbox);
        Require(sourceEntries.Length == ReadonlyTracePrefix817CaseNames.ExpectedQueuedEntries
            && sourceEntries[0].Admission?.Target is PendingAliasAdmission817
            && sourceEntries[1].Admission?.Target is PendingProcessInfoAdmission817
            && sourceEntries[2].Admission?.Target is PendingAliasAdmission817);
        var owners = new[] { BinaryOwner(sourceEntries[0].Value), BinaryOwner(sourceEntries[2].Value) };
        long[] baseline = owners.Select(References).ToArray();
        var states = owners.Select(owner => owner.WritableState).ToArray();
        runtime.ProcessTracing.Configure(
            requesterContext,
            [targetContext.Self, Term.A(ErlangBooleanAtoms.True),
                Term.List(Term.A(ProcessTraceNames.Receive), Term.Tuple(Term.A(ProcessTraceNames.Tracer), requesterContext.Self))]
        );
        bool resume = false;
        try
        {
            // A genuine suspension request holds the original selective info
            // reply receive, so its real queue position remains observable.
            _ = await runtime.Suspensions.Suspend(targetContext, requesterContext.Self, new(true, false, null));
            resume = true;
            await target.InvokeAsync(context =>
            {
                Require(context.Mailbox.Count == ReadonlyTracePrefix817CaseNames.ExpectedTraceCount);

                return Task.CompletedTask;
            });
            await Until(() => runtime.Suspensions.IsActive(requesterContext.Self));
            Require(!query.IsCompleted);
            Mailbox.Entry[] queued = ReadEntries(requesterContext.Mailbox);
            Require(queued.Length == ReadonlyTracePrefix817CaseNames.ExpectedQueuedEntries);
            RequireTrace(queued[0].Value, targetContext.Self, owners[0]);
            Require(queued[1].Value is TupleTerm { Items.Count: ReadonlyTracePrefix817CaseNames.InfoReplyFields } reply
                && reply.Items[0] is ReferenceTerm
                && reply.Items[1].Equals(Term.Tuple(Term.A(ProcessInfoNames.MessageQueueLength), Term.I(1))));
            RequireTrace(queued[2].Value, targetContext.Self, owners[1]);
            Require(!ReferenceEquals(queued[0].Value, queued[2].Value));
            for (int index = 0; index < owners.Length; index++)
                Require(References(owners[index]) == baseline[index] + owners.Count(candidate => ReferenceEquals(candidate, owners[index])) && owners[index].WritableState == states[index]);
            Require(result is null);
            _ = runtime.Suspensions.Resume(targetContext, requesterContext.Self);
            resume = false;
            await query.WaitAsync(ReadonlyTracePrefix817CaseNames.Deadline);
            Require(result is not null && result.Equals(Term.Tuple(Term.A(ProcessInfoNames.MessageQueueLength), Term.I(1))));
            Require(ReadEntries(requesterContext.Mailbox).Length == ReadonlyTracePrefix817CaseNames.ExpectedTraceCount);
            await requester.InvokeAsync(async context =>
            {
                for (int index = 0; index < ReadonlyTracePrefix817CaseNames.ExpectedTraceCount; index++)
                    Require(await context.ReceiveAsync<Term>(value => value, ReadonlyTracePrefix817CaseNames.Deadline) is TupleTerm);
            });
            await target.InvokeAsync(async context =>
            {
                for (int index = 0; index < ReadonlyTracePrefix817CaseNames.ExpectedTraceCount; index++)
                    Require(await context.ReceiveAsync<Term>(value => value, ReadonlyTracePrefix817CaseNames.Deadline) is TupleTerm);
            });
        }
        finally
        {
            if (resume) _ = runtime.Suspensions.Resume(targetContext, requesterContext.Self);
            runtime.ProcessTracing.Configure(requesterContext,
                [targetContext.Self, Term.A(ErlangBooleanAtoms.False), Term.List(Term.A(ProcessTraceNames.Receive))]);
            // Failure cleanup uses the genuine untraced admission path so the
            // original request cannot strand actor disposal behind a trace bug.
            await target.InvokeAsync(context =>
            {
                _ = context.Mailbox.Count;

                return Task.CompletedTask;
            });
            // Always let the real request/receive finish, including failure;
            // do not strand its producer or substitute a synthetic reply.
            await query.WaitAsync(ReadonlyTracePrefix817CaseNames.Deadline);
        }
    }

    private static void RequireTrace(Term value, Pid target, OrdinaryBinaryBacking source)
    {
        Require(value is TupleTerm { Items.Count: ReadonlyTracePrefix817CaseNames.TraceFields });
        var tuple = (TupleTerm)value;
        Require(tuple.Items[0].Equals(Term.A(ProcessTraceNames.Message)) && tuple.Items[1].Equals(target)
            && tuple.Items[2].Equals(Term.A(ProcessTraceNames.Receive)));
        var block = MailboxBackingProbe.Storage(value)!;
        var heap = (ProcessTermHeap506)block.Owner;
        Require(heap.PhysicalWordSnapshot526.LivePhysicalWords == ReadonlyTracePrefix817CaseNames.OutputPhysicalWords
            && ReferenceEquals(BinaryOwner(tuple.Items[3]), source));
    }

    private static OrdinaryBinaryBacking BinaryOwner(Term value)
        => (OrdinaryBinaryBacking)OrdinaryBinaryTestAccess.Owner((BitString)((TupleTerm)value).Items[1])!;

    private static long References(OrdinaryBinaryBacking owner)
        => Marshal.ReadInt64(owner.BinaryPointer, ReadonlyTrace817CaseNames.ReferencesByteOffset);

    private static Mailbox.Entry[] ReadEntries(Mailbox mailbox)
    {
        object gate = typeof(Mailbox).GetField(MailboxBackingCaseNames.GateField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mailbox)!;
        lock (gate)
            return ((IEnumerable)typeof(Mailbox).GetField(MailboxBackingCaseNames.MessagesField,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mailbox)!).Cast<Mailbox.Entry>().ToArray();
    }

    private static bool PendingInfo(ProcessRuntime runtime, ProcessContext target)
    {
        object gate = typeof(ProcessRuntime).GetField(ReadonlyTrace817CaseNames.RuntimeGateField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        lock (gate) return target.PendingInfoRequests.Count == 1;
    }

    private static async Task Until(Func<bool> predicate)
    {
        long start = Stopwatch.GetTimestamp();
        while (!predicate())
        {
            if (Stopwatch.GetElapsedTime(start) > ReadonlyTracePrefix817CaseNames.Deadline)
                throw new InvalidOperationException(ReadonlyTracePrefix817CaseNames.Failure);
            await Task.Yield();
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(ReadonlyTracePrefix817CaseNames.Failure);
    }
}
