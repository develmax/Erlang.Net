using System.Reflection;

namespace Erlang.NifNativeBackend;

// Actual source token, prepared native mailbox graph, global replacement and
// final synchronous R-held commit. No synthetic carrier/recipient authority.
internal static class SequenceSelection170Checks
{
    internal static async Task Run()
    {
        foreach (bool owned in new[] { false, true })
            foreach (ProcessMessageQueueDataMode411 mode in new[] { ProcessMessageQueueDataMode411.OffHeap, ProcessMessageQueueDataMode411.OnHeap })
                await Exercise(owned, mode);
    }

    private static async Task Exercise(bool owned, ProcessMessageQueueDataMode411 mode)
    {
        await using var runtime = NativeOwned516Profile.Create(owned);
        await using var oldTracer = new ReadonlyTrace817Actor(runtime, mode);
        await using var newTracer = new ReadonlyTrace817Actor(runtime, mode);
        await using var sender = new ReadonlyTrace817Actor(runtime, ProcessMessageQueueDataMode411.OffHeap);
        ProcessContext oldContext = await oldTracer.Ready;
        ProcessContext newContext = await newTracer.Ready;
        object gate = typeof(ProcessRuntime).GetField(SequenceSelection170Names.RuntimeGate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        await sender.InvokeAsync(async context =>
        {
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Label),
                Term.I(SequenceSelection170Names.Label)
            );
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Send),
                Term.A(ErlangBooleanAtoms.True)
            );
            await Call(
                context,
                SequenceTraceNames.SystemFlag,
                Term.A(SequenceTraceNames.SystemTracer),
                oldContext.Self
            );
            Term payload = Term.Tuple(Term.A(SequenceSelection170Names.Payload), Term.I(1));
            await using (PreparedOrdinarySequenceSend prepared = await PreparedOrdinarySequenceSend.PrepareAsync(context, newContext.Self, payload))
            {
                Require(prepared.Output is not null && ReferenceEquals(prepared.Output.Recipient, oldContext));
                await Call(
                    context,
                    SequenceTraceNames.SystemFlag,
                    Term.A(SequenceTraceNames.SystemTracer),
                    newContext.Self
                );
                lock (gate)
                {
                    Require(prepared.IsCurrent());
                    prepared.Commit();
                }
            }
            Term expected = Term.Tuple(
                Term.A(SequenceTraceNames.Message),
                Term.I(SequenceSelection170Names.Label),
                Term.Tuple(
                    Term.A(SequenceTraceNames.Send),
                    Term.Tuple(Term.I(0), Term.I(1)),
                    context.Self,
                    newContext.Self,
                    payload
                )
            );
            IReadOnlyList<Term> oldMessages = oldContext.Mailbox.Snapshot();
            Require(oldMessages.Count == 1 && oldMessages[0].Equals(expected)
                && ExternalTermFormat.Encode(oldMessages[0]).AsSpan().SequenceEqual(ExternalTermFormat.Encode(expected))
                && newContext.Mailbox.Snapshot().Count == 0);
            // Captured-none remains suppressed even when a tracer appears
            // before actual commit. Neither selector is the sending process.
            await Call(
                context,
                SequenceTraceNames.SystemFlag,
                Term.A(SequenceTraceNames.SystemTracer),
                Term.A(ErlangBooleanAtoms.False)
            );
            await using (PreparedOrdinarySequenceSend suppressed = await PreparedOrdinarySequenceSend.PrepareAsync(context, newContext.Self, payload))
            {
                Require(suppressed.Output is null);
                await Call(
                    context,
                    SequenceTraceNames.SystemFlag,
                    Term.A(SequenceTraceNames.SystemTracer),
                    newContext.Self
                );
                lock (gate)
                {
                    Require(suppressed.IsCurrent());
                    suppressed.Commit();
                }
            }
            Require(oldContext.Mailbox.Snapshot().Count == 1 && newContext.Mailbox.Snapshot().Count == 0);
        });
    }

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            function,
            arguments
        );

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(SequenceSelection170Names.Failure);
    }
}
