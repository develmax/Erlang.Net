namespace Erlang;

public sealed partial class Mailbox
{
    internal bool HasPendingBacking
    {
        get
        {
            lock (gate)
                return !Volatile.Read(ref closed593)
                    && pendingSignals428.Any(index => index.Value.PendingBacking);
        }
    }

    internal MailboxPendingBackingClaim? ClaimPendingBacking()
    {
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || policyMoveReady || pendingSignals428.First is not { } first)
                return null;
            var node = first.Value;
            if (!node.Value.PendingBacking || node.Value.Admission is null || node.Value.Deferred)
                return null;
            var claim = new MailboxPendingBackingClaim(this, node);
            claim.RetainSource(); // Actual existing custody, before leaving M.

            return claim;
        }
    }

    // R held by actual runtime owner; old claim stays retained until original
    // Admission and all its reentrant effects have completed under R/M.
    internal bool PreparePendingValue(
        MailboxPendingBackingClaim claim,
        PreparedMailboxMessage? prepared,
        MailboxPublicationScope593? publication,
        Term? portableRaw = null
    )
    {
        lock (gate)
        {
            var node = claim.Node;
            if (!ReferenceEquals(claim.Owner, this) || Volatile.Read(ref closed593)
                || !ReferenceEquals(node.List, messages) || !node.Value.PendingBacking
                || !ReferenceEquals(node.Value.Admission, claim.Original.Admission)
                || !ReferenceEquals(node.Value.Custody593, claim.Original.Custody593)
                || !ReferenceEquals(node.Value.Value, claim.Original.Value))
                return false;
            if (prepared is null)
                node.Value = node.Value with { PendingBacking = false };
            else
            {
                var next = PrepareCustody593(publication);
                var previous = node.Value.Custody593;
                node.Value = node.Value with
                {
                    Value = prepared.Value,
                    TraceToken = prepared.Token,
                    DistributionTraceToken = portableRaw,
                    PreserveTrace = prepared.Token is not null && portableRaw is not null ? false : node.Value.PreserveTrace,
                    Custody593 = next,
                    PendingBacking = false
                };
                next?.Publish();
                previous?.Dispose(); // Metadata scheduling; claim still holds source.
            }
            mutationRevision++;
            precedenceRevision++;

            return true;
        }
    }
}
