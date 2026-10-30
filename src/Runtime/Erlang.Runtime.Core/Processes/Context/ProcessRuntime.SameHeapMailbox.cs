namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<bool> TrySendSelfOnHeapMailboxAsync(ProcessContext context, Term target, Term message)
    {
        if (!context.OwnedExecutionEnabled683 || !RuntimeActorExecutionTurn.IsOwnedBy(context)
            || context.CurrentOwned506 is not { IsDisposed: false, CanBorrow507: true } invocation
            || invocation.BindingKind507 == OwnedBindingKind507.Opaque
            || System.Threading.Monitor.IsEntered(gate) || context.Mailbox.DeferredGateHeld
            || ProcessTermHeap506.Storage(message) is not { } storage
            || !ReferenceEquals(storage.Owner, context.TermHeap506)
            || !MailboxValueEscape.CanMaterialize(this, message))
            return false;
        long policy;
        lock (gate)
        {
            if (!Eligible()) return false;
            policy = context.Mailbox.MessageQueuePolicyRevision;
        }
        PreparedMailboxMessage prepared;
        try
        {
            prepared = await mailboxRoots593.PrepareSameHeapMessageAsync(context, message).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            lock (gate) if (!Eligible()) return false;
            throw;
        }
        Exception? primary = null;
        try
        {
            using var publication = MailboxPublicationScope593.Forward(this, prepared.Graph);
            lock (gate)
            {
                if (!Eligible() || context.Mailbox.MessageQueuePolicyRevision != policy) return false;
                context.SavedCalls.Sent();
                // Preserve exact Value/SubBits identity and writer flags. This
                // actual from==Owner, priority=false,token=null path does NOT pin.
                context.Mailbox.SendTracedSnapshot593(
                    message,
                    null,
                    from: context.Self,
                    publication593: publication
                );

                return true;
            }
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            try
            {
                await prepared.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup) when (primary is not null && !ReferenceEquals(primary, cleanup))
            {
                throw new AggregateException(primary, cleanup);
            }
        }

        bool Eligible()
            => ReferenceEquals(context.Runtime, this) && !disposed && !IsHaltRequested && !context.Terminated
                && context.SequentialTrace.Token is null && !context.Mailbox.CopySelfMessages
                && context.Mailbox.MessageQueueData411 == ProcessMessageQueueDataMode411.OnHeap
                && ProcessTracing.CanPublishDirectMailboxBacking()
                && ResolveBackedMailboxTarget(target, out var actual) && actual.Equals(context.Self)
                && processes.TryGetValue(context.Self, out var owner) && ReferenceEquals(owner, context);
    }
}
