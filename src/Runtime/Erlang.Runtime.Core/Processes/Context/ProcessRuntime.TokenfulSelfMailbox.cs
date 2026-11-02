namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Called ONLY after the same physical actor/context/frame/root validation
    // in TrySendSelfOnHeapMailboxAsync, before generic Export/Graph641.
    private async ValueTask<bool> TrySendTokenfulSelfOnHeapMailboxAsync(
        ProcessContext context,
        Term target,
        Term message
    )
    {
        while (true)
        {
            SequenceTraceSendPreview preview;
            ProcessTermBorrow506? borrow;
            long policy;
            lock (gate)
            {
                if (!Eligible()) return false;
                preview = SequenceTracing.PreviewSent(context);
                borrow = context.SequentialTrace.BorrowPhysical();
                policy = context.Mailbox.MessageQueuePolicyRevision;
            }
            PreparedMailboxMessage? prepared = null;
            PreparedSequenceTraceOutput? output = null;
            Exception? primary = null;
            try
            {
                using (borrow)
                {
                    prepared = await mailboxRoots593.PrepareTokenfulSelfMessageAsync(
                        context, message, preview.Next!
                    ).ConfigureAwait(false);
                    if ((preview.Next!.Flags & SequenceTraceFlags.Send) != 0)
                        output = await SequenceTracing.PrepareOutputAsync(
                            [preview.Next.Label, preview.Next.Sender, context.Self, message], context.Self
                        ).ConfigureAwait(false);
                }
                // The source physical borrow has ended BEFORE publication.
                using var publication = MailboxPublicationScope593.Forward(this, prepared.Graph);
                lock (gate)
                {
                    if (!Eligible()) return false;
                    if (context.Mailbox.MessageQueuePolicyRevision != policy
                        || !SequenceTracing.IsCurrentSendPreview(context, preview)
                        || !SequenceTracing.IsCurrentOutputPlanOrSuppressed(
                            output,
                            preview.Next!,
                            SequenceTraceFlags.Send,
                            context.Self
                        )) continue;
                    context.SavedCalls.Sent();
                    _ = SequenceTracing.SentPrepared(
                        context,
                        context.Self,
                        message,
                        output
                    );
                    context.Mailbox.SendTracedSnapshot593(
                        prepared.Value,
                        prepared.Token,
                        from: context.Self,
                        publication593: publication
                    );

                    return true;
                }
            }
            catch (ObjectDisposedException error)
            {
                lock (gate) if (!Eligible()) return false;
                primary = error;
                throw;
            }
            catch (Exception error)
            {
                primary = error;
                throw;
            }
            finally
            {
                Exception? failure = null;
                try
                {
                    if (output is not null) await output.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    failure = error;
                }
                try
                {
                    if (prepared is not null) await prepared.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception cleanup)
                {
                    failure = failure is null ? cleanup : new AggregateException(failure, cleanup);
                }
                if (failure is not null)
                {
                    if (primary is not null) throw new AggregateException(primary, failure);
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
                }
            }
        }

        bool Eligible()
            => ReferenceEquals(context.Runtime, this) && !disposed && !IsHaltRequested && !context.Terminated
                && context.SequentialTrace.Token is not null && !context.Mailbox.CopySelfMessages
                && context.Mailbox.MessageQueuePlacementMode == ProcessMessageQueueDataMode411.OnHeap
                && ProcessTracing.CanPublishDirectMailboxBacking() && ProcessTracing.SupportsMessageSnapshots593()
                && ResolveBackedMailboxTarget(target, out var actual) && actual.Equals(context.Self)
                && processes.TryGetValue(context.Self, out var owner) && ReferenceEquals(owner, context);
    }
}
