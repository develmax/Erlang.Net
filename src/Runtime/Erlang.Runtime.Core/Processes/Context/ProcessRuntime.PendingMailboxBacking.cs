using System.Runtime.ExceptionServices;

namespace Erlang;

public sealed partial class ProcessRuntime
{
    private MailboxSignalBatch428 PrepareAndAdmitPendingMailboxBacking(ProcessContext owner,
        MailboxPendingBackingClaim claim, int? budget)
    {
        PreparedMailboxMessage? prepared = null;
        MailboxPublicationScope593? publication = null;
        Exception? failure = null;
        MailboxSignalBatch428 result = default;
        PendingSequenceTerminationControl? terminal = FindPendingSequenceTermination(claim);
        try
        {
            result = PrepareAndAdmit();
        }
        catch (Exception error)
        {
            Add(error);
        }
        finally
        {
            try
            {
                terminal?.ReleaseOutsideRuntimeGate();
            }
            catch (Exception error)
            {
                Add(error);
            }
            // Retain original actual source through original Admission, including
            // reentrant Terminate that captures the existing reason641 graph.
            try
            {
                claim.Dispose();
            }
            catch (Exception error)
            {
                Add(error);
            }
            try
            {
                publication?.Dispose();
            }
            catch (Exception error)
            {
                Add(error);
            }
            try
            {
                prepared?.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (Exception error)
            {
                Add(error);
            }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();

        return result;

        void Add(Exception error)
        {
            if (failure is null) failure = error;
            else if (!ReferenceEquals(failure, error)) failure = new AggregateException(failure, error);
        }

        bool Live()
            => !owner.Terminated && ReferenceEquals(claim.Owner, owner.Mailbox)
                && processes.TryGetValue(owner.Self, out var actual) && ReferenceEquals(actual, owner);

        MailboxSignalBatch428 Admit()
        {
            if (budget is int maximum)
                return owner.Mailbox.AdmitPendingBounded428(maximum);
            owner.Mailbox.AdmitPending();

            return new(0, false, owner.Mailbox.HasPendingSignals428);
        }

        MailboxSignalBatch428 PrepareAndAdmit()
        {
            bool materializable = MailboxValueEscape.CanMaterialize(this, claim.Value)
                && (claim.Original.DistributionTraceToken is not { } rawInput || MailboxValueEscape.CanMaterialize(this, rawInput));
            // A legacy naked physical token is not proved rooted by Value-only
            // custody. Known767 Value+Token fragments supply the actual source.
            bool tokenRooted = claim.Original.TraceToken is null
                || ReferenceEquals(claim.Original.Custody593?.RecipientFragment?.Token, claim.Original.TraceToken);
            Term? portableRaw = claim.Original.DistributionTraceToken;
            while (true)
            {
                terminal?.ReleaseOutsideRuntimeGate();
                terminal?.PrepareOutsideRuntimeGate();
                ProcessMessageQueueDataMode411 mode;
                long revision;
                lock (gate)
                {
                    if (!Live()) return default;
                    if (terminal is not null && !terminal.IsCurrent()) continue;
                    // Preserve old unsupported/traced/shutdown delivery. Do not
                    // turn data into actor invocation authority or fresh roots.
                    if (!materializable || disposed || IsHaltRequested
                        || !ProcessTracing.CanPublishDirectMailboxBacking()
                        || !tokenRooted)
                    {
                        if (!owner.Mailbox.PreparePendingValue(claim, null, null)) return default;

                        return Admit();
                    }
                    mode = owner.Mailbox.MessageQueuePlacementMode;
                    revision = owner.Mailbox.MessageQueuePolicyRevision;
                }
                try
                {
                    if (claim.Original.DistributionTraceToken is { } raw)
                    {
                        var rawPrepared = mailboxRoots593.PrepareRawDistributionMailboxMessageAsync(
                            this,
                            owner,
                            mode,
                            claim.Value,
                            raw,
                            claim.Original.Custody593?.Graph?.LiteralRoots801,
                            claim.RetainedFragmentSource
                        ).GetAwaiter().GetResult();
                        prepared = rawPrepared.Message;
                        portableRaw = rawPrepared.PortableRaw;
                    }
                    else
                        prepared = (claim.Original.TraceToken is { } token
                            ? mailboxRoots593.PrepareMailboxMessageAsync(
                                this,
                                owner,
                                mode,
                                claim.Value,
                                token,
                                claim.Original.Custody593?.Graph?.LiteralRoots801,
                                claim.RetainedFragmentSource
                            )
                            : mailboxRoots593.PrepareMailboxMessageWithLiteral824Async(
                                this,
                                owner,
                                mode,
                                claim.Value,
                                claim.Original.Custody593?.Graph?.LiteralRoots801,
                                claim.RetainedFragmentSource
                            )).GetAwaiter().GetResult();
                }
                catch (ObjectDisposedException)
                {
                    lock (gate)
                    {
                        if (!Live()) return default;
                        if (terminal is not null && !terminal.IsCurrent()) continue;
                        if (disposed || IsHaltRequested || owner.Mailbox.IsClosedForBacking)
                        {
                            if (!owner.Mailbox.PreparePendingValue(claim, null, null)) return default;

                            return Admit();
                        }
                    }
                    throw;
                }
                publication = MailboxPublicationScope593.Forward(this, prepared.Graph);
                bool retry;
                lock (gate)
                {
                    if (!Live()) return default;
                    retry = terminal is not null && !terminal.IsCurrent()
                        || owner.Mailbox.MessageQueuePlacementMode != mode
                        || owner.Mailbox.MessageQueuePolicyRevision != revision;
                    if (!retry)
                    {
                        if (disposed || IsHaltRequested || !ProcessTracing.CanPublishDirectMailboxBacking())
                        {
                            if (!owner.Mailbox.PreparePendingValue(claim, null, null)) return default;
                        }
                        else if (!owner.Mailbox.PreparePendingValue(
                            claim,
                            prepared,
                            publication,
                            portableRaw
                        ))
                            return default;

                        // No R gap between exact physical Value swap and old
                        // callback effects (monitor/alias/link removal/kill).
                        return Admit();
                    }
                }
                publication.Dispose();
                publication = null;
                prepared.DisposeAsync().GetAwaiter().GetResult();
                prepared = null;
            }
        }
    }
}
