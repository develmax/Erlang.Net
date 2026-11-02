using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Erlang.NifNativeBackend;

// Deterministic transition windows use real actors and the actual prepared
// fragment. No matcher, token, queue, native retain or publication hook is faked.
internal static class ReadonlyTraceTransition304Checks
{
    internal static async Task Run()
    {
        foreach (bool owned in new[] { false, true })
            foreach (ReadonlyTraceTransition304Kind kind in Enum.GetValues<ReadonlyTraceTransition304Kind>())
                await One(owned, kind);
    }

    private static async Task One(bool owned, ReadonlyTraceTransition304Kind kind)
    {
        await using var runtime = NativeOwned516Profile.Create(owned);
        var sourceActor = new ReadonlyTrace817Actor(runtime, ProcessMessageQueueDataMode411.OffHeap);
        var recipient = new ReadonlyTrace817Actor(runtime, ProcessMessageQueueDataMode411.OffHeap);
        ReadonlyTraceSource817? source = null;
        ReadonlyTraceOutput817? output = null;
        OrdinaryBinaryBacking? native = null;
        List<Exception> failures = new();
        try
        {
            ProcessContext sender = await sourceActor.Ready.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
            ProcessContext target = await recipient.Ready.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
            byte[] bytes = Enumerable.Repeat(ReadonlyTraceTransition304Names.PayloadByte, ReadonlyTrace817CaseNames.BinaryBytes).ToArray();
            ReferenceTerm alias = runtime.CreateAlias(sender, false, false);
            runtime.Send(alias, Term.Tuple(Term.A(ReadonlyTrace817CaseNames.Payload), new BitString(bytes)));
            var original = MailboxBackingProbe.ReadSingle(sender.Mailbox);
            source = ReadonlyTraceSource817.CaptureEntry(sender, ReadCustody(sender.Mailbox), original.Value);
            Require(source is not null && source.ValidateOutsideGates());
            native = (OrdinaryBinaryBacking)OrdinaryBinaryTestAccess.Owner((BitString)((TupleTerm)original.Value).Items[1])!;
            long baseline = References(native);
            var originalState = native.WritableState;
            output = await runtime.MailboxRoots593.ReserveReadonlyTraceOutput817Async(sender, target, source!);
            Term reserved = output.ActualValue817;
            await sourceActor.InvokeAsync(context =>
            {
                _ = context.CollectOwnedGeneration508(true);

                return Task.CompletedTask;
            });
            Require(ReferenceEquals(MailboxBackingProbe.ReadSingle(sender.Mailbox).Value, original.Value)
                && References(native) == baseline && native.WritableState == originalState);
            Configure(
                runtime,
                target,
                sender.Self,
                target.Self,
                true
            );
            TupleTerm? changedToken = null;
            if (kind == ReadonlyTraceTransition304Kind.RecipientDeath)
            {
                // Recipient was genuinely live when its output was reserved.
                await recipient.DisposeAsync();
            }
            else
            {
                var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Task changeToken = recipient.InvokeAsync(async context =>
                {
                    await start.Task;
                    runtime.SequenceTracing.Set(context, Term.A(SequenceTraceNames.Label), Term.I(ReadonlyTraceTransition304Names.ChangedTokenLabel));
                    changedToken = context.SequentialTrace.Token!.PhysicalTuple!;
                    if (kind == ReadonlyTraceTransition304Kind.LivePlacementAndToken)
                        context.Mailbox.ExchangeMessageQueueData411(ProcessMessageQueueDataMode411.OnHeap);
                });
                Task changeRoute = sourceActor.InvokeAsync(async context =>
                {
                    await start.Task;
                    if (kind == ReadonlyTraceTransition304Kind.SelfTracerAndToken)
                    {
                        Configure(
                            runtime,
                            context,
                            context.Self,
                            target.Self,
                            false
                        );
                        Configure(
                            runtime,
                            context,
                            context.Self,
                            context.Self,
                            true
                        );
                    }
                });
                start.TrySetResult();
                await Task.WhenAll(changeToken, changeRoute).WaitAsync(ReadonlyTrace817CaseNames.Deadline);
            }
            ReadonlyTraceOutput817 selectedOutput = output;
            await sourceActor.InvokeAsync(context =>
            {
                using var scope = new ReadonlyTraceOutputScope817(runtime.ProcessTracing, selectedOutput);
                Require(context.Mailbox.Count == 1);
                Require(scope.Consumed817 == (kind == ReadonlyTraceTransition304Kind.LivePlacementAndToken));

                return Task.CompletedTask;
            });
            if (kind == ReadonlyTraceTransition304Kind.LivePlacementAndToken)
            {
                var queued = MailboxBackingProbe.ReadSingle(target.Mailbox);
                Require(ReferenceEquals(queued.Value, reserved) && References(native) == baseline + 1
                    && native.WritableState == originalState);
            }
            else
                Require(QueuedEntries(target.Mailbox) == 0 && References(native) == baseline && native.WritableState == originalState);
            await output.DisposeAsync();
            output = null;
            source!.Dispose();
            source = null;
            // Remove the actual source queue roots. No host binary DTO is used
            // as a substitute for the source process's real native custody.
            await sourceActor.DisposeAsync();
            await original.Work!.Completion.Task.WaitAsync(ReadonlyTrace817CaseNames.Deadline);
            if (kind == ReadonlyTraceTransition304Kind.LivePlacementAndToken)
            {
                Require(!native.Retired.IsCompleted);
                await recipient.InvokeAsync(context =>
                {
                    _ = context.CollectOwnedGeneration508(true);
                    var queued = MailboxBackingProbe.ReadSingle(context.Mailbox);
                    Require(ReferenceEquals(MailboxBackingProbe.Storage(queued.Value)!.Owner, context.TermHeap506));
                    Require(ReferenceEquals(context.SequentialTrace.Token!.PhysicalTuple, changedToken)
                        && context.SequentialTrace.Token.Label.Equals(Term.I(ReadonlyTraceTransition304Names.ChangedTokenLabel)));

                    return Task.CompletedTask;
                });
                await recipient.InvokeAsync(async context =>
                {
                    Term? trace = await context.ReceiveAsync<Term>(value => value, ReadonlyTrace817CaseNames.Deadline);
                    Require(trace is TupleTerm tuple && tuple.Items.Count == 4
                        && tuple.Items[0].Equals(Term.A(ProcessTraceNames.Message))
                        && tuple.Items[1].Equals(sender.Self)
                        && tuple.Items[2].Equals(Term.A(ProcessTraceNames.Receive))
                        && ((BitString)((TupleTerm)tuple.Items[3]).Items[1]).ToArray().SequenceEqual(bytes));
                    Require(ReferenceEquals(context.SequentialTrace.Token!.PhysicalTuple, changedToken));
                });
                await recipient.InvokeAsync(context =>
                {
                    _ = context.CollectOwnedGeneration508(true);

                    return Task.CompletedTask;
                });
            }
            else if (kind == ReadonlyTraceTransition304Kind.SelfTracerAndToken)
            {
                await recipient.InvokeAsync(context =>
                {
                    Require(ReferenceEquals(context.SequentialTrace.Token!.PhysicalTuple, changedToken)
                        && context.SequentialTrace.Token.Label.Equals(Term.I(ReadonlyTraceTransition304Names.ChangedTokenLabel)));

                    return Task.CompletedTask;
                });
            }
            await recipient.DisposeAsync();
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
                if (output is not null) await output.DisposeAsync();
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
                await sourceActor.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await recipient.DisposeAsync();
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

    private static void Configure(
        ProcessRuntime runtime,
        ProcessContext caller,
        Pid tracee,
        Pid tracer,
        bool enabled
    )
        => runtime.ProcessTracing.Configure(
            caller,
            [tracee, Term.A(enabled ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
                Term.List(Term.A(ProcessTraceNames.Receive), Term.Tuple(Term.A(ProcessTraceNames.Tracer), tracer))]
        );

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

    private static int QueuedEntries(Mailbox mailbox)
    {
        object gate = typeof(Mailbox).GetField(MailboxBackingCaseNames.GateField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mailbox)!;
        lock (gate)
            return ((IEnumerable)typeof(Mailbox).GetField(MailboxBackingCaseNames.MessagesField,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mailbox)!).Cast<object>().Count();
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(ReadonlyTraceTransition304Names.Failure);
    }
}
