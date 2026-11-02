using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

// The positive path is a real pending alias admission and ProcessTracing
// Receive selection. General prefix claiming/matcher parity remain separate.
public static class ReadonlyTrace817Checks
{
    public static async Task RealCellsLateShareDiscardAndCollectorImport()
    {
        await ReadonlyTraceTransition304Checks.Run();

        foreach (bool owned in new[] { false, true })
        {
            await FilteredEvent(owned);
            foreach (ProcessMessageQueueDataMode411 mode in new[] { ProcessMessageQueueDataMode411.OffHeap, ProcessMessageQueueDataMode411.OnHeap })
                await Exercise(owned, mode);
        }
    }

    private static async Task FilteredEvent(bool owned)
    {
        await using var runtime = NativeOwned516Profile.Create(owned);
        await using var receiver = new ReadonlyTrace817Actor(runtime, ProcessMessageQueueDataMode411.OffHeap);
        await using var tracer = new ReadonlyTrace817Actor(runtime, ProcessMessageQueueDataMode411.OnHeap);
        ProcessContext receiverContext = await receiver.Ready.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
        ProcessContext tracerContext = await tracer.Ready.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
        Term input = Term.Tuple(Term.A(ReadonlyTrace817CaseNames.Payload), new BitString(new byte[ReadonlyTrace817CaseNames.BinaryBytes]));
        ReferenceTerm alias = runtime.CreateAlias(receiverContext, false, false);
        runtime.Send(alias, input);
        var original = MailboxBackingProbe.ReadSingle(receiverContext.Mailbox);
        using ReadonlyTraceSource817 source = ReadonlyTraceSource817.CaptureEntry(receiverContext,
            ReadCustody(receiverContext.Mailbox), original.Value)!;
        Require(source.ValidateOutsideGates());
        var native = (OrdinaryBinaryBacking)OrdinaryBinaryTestAccess.Owner((BitString)((TupleTerm)original.Value).Items[1])!;
        long baseline = References(native);
        var state = native.WritableState;
        long tracerWords = tracerContext.TermHeap506.PhysicalWordSnapshot526.LivePhysicalWords;
        await using ReadonlyTraceOutput817 output = await runtime.MailboxRoots593.ReserveReadonlyTraceOutput817Async(
            receiverContext, tracerContext, source);
        runtime.ProcessTracing.Configure(
            tracerContext,
            [receiverContext.Self, Term.A(ErlangBooleanAtoms.True),
                Term.List(Term.A(ProcessTraceNames.Receive), Term.Tuple(Term.A(ProcessTraceNames.Tracer), tracerContext.Self))]
        );
        Term spec = Term.List(Term.Tuple(
            Term.A(MatchSpecNames.Wildcard),
            Nil.Value,
            Term.List(Term.Tuple(Term.A(MatchSpecNames.Message), Term.A(ErlangBooleanAtoms.False)))
        ));
        runtime.ProcessTracing.Pattern(tracerContext, [Term.A(ProcessTraceNames.Receive), spec, Nil.Value]);
        await receiver.InvokeAsync(context =>
        {
            using var scope = new ReadonlyTraceOutputScope817(runtime.ProcessTracing, output);
            Require(context.Mailbox.Count == 1 && !scope.Consumed817);

            return Task.CompletedTask;
        });
        Require(References(native) == baseline && native.WritableState == state
            && tracerContext.TermHeap506.PhysicalWordSnapshot526.LivePhysicalWords == tracerWords
            && tracerContext.Mailbox.Snapshot().Count == 0);
    }

    private static async Task Exercise(bool owned, ProcessMessageQueueDataMode411 mode)
    {
        await using var runtime = NativeOwned516Profile.Create(owned);
        var receiver = new ReadonlyTrace817Actor(runtime, ProcessMessageQueueDataMode411.OffHeap);
        var tracer = new ReadonlyTrace817Actor(runtime, mode);
        ReadonlyTraceSource817? source = null;
        ReadonlyTraceOutput817? discarded = null, selected = null;
        OrdinaryBinaryBacking? native = null;
        List<Exception> failures = new();
        try
        {
            ProcessContext receiverContext = await receiver.Ready.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
            ProcessContext tracerContext = await tracer.Ready.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
            runtime.SequenceTracing.Set(tracerContext, Term.A(SequenceTraceNames.Label), Term.I(ReadonlyTrace817CaseNames.PreservedTokenLabel));
            TupleTerm preservedToken = tracerContext.SequentialTrace.Token!.PhysicalTuple!;
            long tracerBaseline = tracerContext.TermHeap506.PhysicalWordSnapshot526.LivePhysicalWords;
            Term input = Term.Tuple(Term.A(ReadonlyTrace817CaseNames.Payload), new BitString(new byte[ReadonlyTrace817CaseNames.BinaryBytes]));
            ReferenceTerm alias = runtime.CreateAlias(receiverContext, false, false);
            runtime.Send(alias, input);
            var original = MailboxBackingProbe.ReadSingle(receiverContext.Mailbox);
            source = ReadonlyTraceSource817.CaptureEntry(receiverContext, ReadCustody(receiverContext.Mailbox), original.Value);
            Require(source is not null && source.ValidateOutsideGates());
            BitString actualBinary = (BitString)((TupleTerm)original.Value).Items[1];
            native = (OrdinaryBinaryBacking)OrdinaryBinaryTestAccess.Owner(actualBinary)!;
            long baseline = References(native);
            var originalState = native.WritableState;
            Require(ReadonlyTraceSource817.CaptureEntry(tracerContext, ReadCustody(receiverContext.Mailbox), original.Value) is null
                && References(native) == baseline);
            await using (var dead = new ReadonlyTrace817Actor(runtime, ProcessMessageQueueDataMode411.OffHeap))
            {
                ProcessContext deadContext = await dead.Ready.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
                await dead.DisposeAsync();
                await using ReadonlyTraceOutput817 deadOutput = await runtime.MailboxRoots593.ReserveReadonlyTraceOutput817Async(
                    receiverContext, deadContext, source!);
                object deadGate = typeof(ProcessRuntime).GetField(ReadonlyTrace817CaseNames.RuntimeGateField,
                    BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
                lock (deadGate) Require(!deadOutput.PublishSelected());
                Require(References(native) == baseline && native.WritableState == originalState);
            }
            discarded = await runtime.MailboxRoots593.ReserveReadonlyTraceOutput817Async(receiverContext, tracerContext, source!);
            Term oldDiscardedValue = discarded.ActualValue817;
            Require(MailboxBackingProbe.Storage(oldDiscardedValue) is not null
                && References(native) == baseline && native.WritableState == originalState
                && tracerContext.TermHeap506.PhysicalWordSnapshot526.LivePhysicalWords == tracerBaseline);
            await discarded.DisposeAsync();
            discarded = null;
            Require(References(native) == baseline && native.WritableState == originalState);
            Require(tracerContext.TermHeap506.PhysicalWordSnapshot526.LivePhysicalWords == tracerBaseline);
            bool stale = false;
            try
            {
                _ = ((TupleTerm)oldDiscardedValue).Items[0];
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale);
            selected = await runtime.MailboxRoots593.ReserveReadonlyTraceOutput817Async(receiverContext, tracerContext, source!);
            Require(References(native) == baseline && native.WritableState == originalState);
            Term exactOutput = selected.ActualValue817;
            runtime.ProcessTracing.Configure(
                tracerContext,
                [receiverContext.Self, Term.A(ErlangBooleanAtoms.True),
                    Term.List(Term.A(ProcessTraceNames.Receive), Term.Tuple(Term.A(ProcessTraceNames.Tracer), tracerContext.Self))]
            );
            ReadonlyTraceOutput817 selectedOutput = selected;
            await receiver.InvokeAsync(context =>
            {
                using var scope = new ReadonlyTraceOutputScope817(runtime.ProcessTracing, selectedOutput);
                Require(context.Mailbox.Count == 1 && scope.Consumed817);

                return Task.CompletedTask;
            });
            var queued = MailboxBackingProbe.ReadSingle(tracerContext.Mailbox);
            Require(ReferenceEquals(queued.Value, exactOutput) && References(native) == baseline + 1
                && native.WritableState == originalState);
            ProcessTermHeap506 privateHeap = (ProcessTermHeap506)MailboxBackingProbe.Storage(queued.Value)!.Owner;
            Require(!ReferenceEquals(privateHeap, tracerContext.TermHeap506)
                && privateHeap.PhysicalWordSnapshot526.LivePhysicalWords == ReadonlyTrace817CaseNames.OutputPhysicalWords);
            Require(queued.Value.Equals(Term.Tuple(
                Term.A(ProcessTraceNames.Message),
                receiverContext.Self,
                Term.A(ProcessTraceNames.Receive),
                input
            )));
            await selected.DisposeAsync();
            selected = null;
            source!.Dispose();
            source = null;
            await receiver.DisposeAsync();
            await original.Work!.Completion.Task.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
            Require(References(native) == 1);
            await tracer.InvokeAsync(context =>
            {
                _ = context.CollectOwnedGeneration508(true);

                return Task.CompletedTask;
            });
            var after = MailboxBackingProbe.ReadSingle(tracerContext.Mailbox);
            ProcessTermHeap506 afterHeap = (ProcessTermHeap506)MailboxBackingProbe.Storage(after.Value)!.Owner;
            Require(ReferenceEquals(afterHeap, tracerContext.TermHeap506) == (mode == ProcessMessageQueueDataMode411.OnHeap));
            Require(after.Value.Equals(Term.Tuple(
                Term.A(ProcessTraceNames.Message),
                receiverContext.Self,
                Term.A(ProcessTraceNames.Receive),
                input
            )));
            if (mode == ProcessMessageQueueDataMode411.OffHeap) Require(References(native) == 1);
            // On_heap's generic814 conversion retains its known extra input
            // graph anchor. This test does not call that debt OTP-refcount parity.
            await tracer.InvokeAsync(async context =>
            {
                Term? value = await context.Mailbox.ReceiveAsync<Term>(item => item,
                    ReadonlyTrace817CaseNames.Deadline, context.Cancellation);
                Require(value is TupleTerm && ReferenceEquals(context.SequentialTrace.Token!.PhysicalTuple, preservedToken)
                    && context.SequentialTrace.Token.Label.Equals(Term.I(ReadonlyTrace817CaseNames.PreservedTokenLabel)));
            });
            await after.Work!.Completion.Task.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
            await tracer.InvokeAsync(context =>
            {
                _ = context.CollectOwnedGeneration508(true);
                Require(context.TermHeap506.PhysicalWordSnapshot526.LivePhysicalWords == tracerBaseline);

                return Task.CompletedTask;
            });
            await native.Retired.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                if (discarded is not null) await discarded.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                if (selected is not null) await selected.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                source?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await receiver.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await tracer.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await runtime.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                if (native is not null) await native.Retired.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    private static MailboxEntryCustody593 ReadCustody(Mailbox mailbox)
    {
        object gate = typeof(Mailbox).GetField(MailboxBackingCaseNames.GateField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mailbox)!;
        lock (gate)
        {
            var entries = (IEnumerable)typeof(Mailbox).GetField(MailboxBackingCaseNames.MessagesField,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mailbox)!;
            object entry = entries.Cast<object>().Single();

            return (MailboxEntryCustody593)entry.GetType().GetProperty(MailboxBackingCaseNames.CustodyProperty)!.GetValue(entry)!;
        }
    }

    private static long References(OrdinaryBinaryBacking value)
        => Marshal.ReadInt64(value.BinaryPointer, ReadonlyTrace817CaseNames.ReferencesByteOffset);

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(ReadonlyTrace817CaseNames.Failure);
    }
}
