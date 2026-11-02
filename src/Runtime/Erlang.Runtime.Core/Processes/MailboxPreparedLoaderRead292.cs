namespace Erlang;

// Per-copy loan, never a published/ambient capability. The actual Entry work
// operation, real active graph guest lease, and real native reference words are
// all necessary. A runtime match or disposed Host object is never authority.
internal sealed class MailboxPreparedLoaderRead292 : IDisposable
{
    private readonly ProcessRuntime runtime;
    private readonly Term sourceGraph;
    private readonly MailboxPayloadLease593 work;
    private IDisposable? operation;

    private MailboxPreparedLoaderRead292(
        ProcessRuntime runtime,
        Term sourceGraph,
        MailboxPayloadLease593 work,
        IDisposable operation
    )
    {
        this.runtime = runtime;
        this.sourceGraph = sourceGraph;
        this.work = work;
        this.operation = operation;
    }

    internal static MailboxPreparedLoaderRead292? TryCreate(
        ProcessRuntime runtime,
        Term value,
        MailboxPayloadLease593 work,
        IDisposable? currentOperation
    )
    {
        if (!PreparedLoaderOwner807.AnyLive807 || !HasDisposedHost(value)) return null;
        if (currentOperation is null || !work.OwnsPreparedReadOperation807(currentOperation))
            throw new ObjectDisposedException(nameof(MailboxPreparedLoaderRead292));
        runtime.AssertSequenceTracePreparation();
        runtime.MailboxRoots593.RequirePreparedReadWork292(runtime, work);
        IDisposable retained = work.BorrowOperation();
        try
        {
            return new(
                runtime,
                value,
                work,
                retained
            );
        }
        catch
        {
            retained.Dispose();
            throw;
        }
    }

    private static bool HasDisposedHost(Term root)
    {
        var traversal = new ResourceRootTraversal595(root);
        while (traversal.TryPop(out Term? value))
        {
            if (value is ResourceReferenceTerm
                {
                    ResourceKind: ReferenceResourceKind.PreparedBeam,
                    OwnedStorage562: null, PreparedHost807: { } host
                } reference)
            {
                PreparedLoaderOwner807 owner = reference.PreparedBacking807
                    ?? throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
                if (!host.Owns807(owner)) return true;
            }
            traversal.EnqueueChildren(value);
        }

        return false;
    }

    internal ResourceReferenceTerm CopyHost(ResourceReferenceTerm source, AtomicsHostCopyTransaction591? transaction)
    {
        if (source.OwnedStorage562 is not null || source.PreparedHost807 is not { } oldHost)
            return PreparedLoaderRoots807.HostCopy807(source, transaction);
        PreparedLoaderOwner807 owner = source.PreparedBacking807
            ?? throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        if (oldHost.Owns807(owner)) return PreparedLoaderRoots807.HostCopy807(source, transaction);
        IDisposable liveOperation = Volatile.Read(ref operation)
            ?? throw new ObjectDisposedException(nameof(MailboxPreparedLoaderRead292));
        if (!work.OwnsPreparedReadOperation807(liveOperation))
            throw new ObjectDisposedException(nameof(MailboxPreparedLoaderRead292));
        if (!ReferenceEquals(owner.Runtime807, runtime) || !ReferenceEquals(source.Entry.Owner, runtime.ReferenceResources))
            throw new NotSupportedException(ProcessTerm506Names.ForeignRoot);
        if (!ContainsExactSource(source)) throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        PreparedLoaderLease807 guest = work.FindPreparedGuest807(owner)
            ?? throw new ObjectDisposedException(PreparedLoaderNames807.Closed);
        // ReadNumbers authenticates the actual still-live registered native
        // owner/lease under its gate. The outer operation keeps it alive across
        // concurrent Entry removal or VM cleanup until this copy completes.
        IReadOnlyList<uint> nativeWords = owner.ReadNumbers807(guest);
        if (!nativeWords.SequenceEqual(source.Words)) throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        var identity = new ReferenceTerm(source);
        transaction?.Reserve591();
        var host = new PreparedLoaderHost807(owner); // Retain checks concurrent close normally.
        try
        {
            var result = new ResourceReferenceTerm(source.Entry, identity, host);
            transaction?.TrackPreparedLoader807(host);

            return result;
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }

    private bool ContainsExactSource(ResourceReferenceTerm source)
    {
        var traversal = new ResourceRootTraversal595(sourceGraph);
        while (traversal.TryPop(out Term? value))
        {
            if (ReferenceEquals(value, source)) return true;
            traversal.EnqueueChildren(value);
        }

        return false;
    }

    public void Dispose() => Interlocked.Exchange(ref operation, null)?.Dispose();
}
