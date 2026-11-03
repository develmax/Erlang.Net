namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    internal async ValueTask<ReadonlyTraceOutput817> ReserveReadonlyTraceOutput817Async(
        ProcessContext tracee,
        ProcessContext tracer,
        ReadonlyTraceSource817 source
    )
    {
        ProcessRuntime runtime = tracee.Runtime;
        runtime.AssertSequenceTracePreparation();
        if (tracee.Mailbox.DeferredGateHeld || tracer.Mailbox.DeferredGateHeld)
            throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        source.RequireValidated();
        if (!ReferenceEquals(tracer.Runtime, runtime) || !ReferenceEquals(source.Runtime, runtime)
            || !ReferenceEquals(source.Receiver817, tracee)
            || ReferenceEquals(tracer, tracee)) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        ReadonlyTraceSource817 held = source.Retain();
        var work = new MailboxPayloadLease593(this);
        bool registered = false;
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closed, this);
                active.AddLast(work.Link);
                registered = true;
                work.MarkRegistered593();
            }
            var prepared = await MailboxMessageFragment.ReserveReadonlyTrace817Async(tracee, source).ConfigureAwait(false);
            work.AttachRecipientPreparation(prepared.Fragment, tracer.Mailbox.PayloadRetirements593);
            var graph = new MailboxGraphSnapshot593(runtime, work);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);

            return new(
                runtime,
                tracer,
                graph,
                prepared.Fragment.Value,
                prepared.Leases,
                held
            );
        }
        catch (Exception original)
        {
            Exception? cleanup = null;
            try
            {
                if (registered)
                {
                    work.ReleaseOwner();
                    await MailboxRootSettlement593.SettleAsync(work).ConfigureAwait(false);
                }
            }
            catch (Exception failure)
            {
                cleanup = failure;
            }
            finally
            {
                held.ReleaseUse();
            }
            if (cleanup is not null) throw new AggregateException(original, cleanup);
            throw;
        }
    }
}
