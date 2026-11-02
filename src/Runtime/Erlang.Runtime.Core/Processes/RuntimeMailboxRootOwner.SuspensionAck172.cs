namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    internal SuspensionAckTag172 PrepareSuspensionAck172(ProcessRuntime runtime, PortableControlTerm826 portable)
    {
        runtime.AssertSequenceTracePreparation();
        Term source = portable.Value;
        Term complete = SuspensionAckMetadataRoots172(source);
        EnsureSuspensionAckCleanup172();
        var reservations = new List<NativeResourceRootLease524>();
        var work = new MailboxPayloadLease593(this);
        work.MarkSuspensionAckCleanup172();
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            active.AddLast(work.Link);
            work.MarkRegistered593();
        }
        bool emptyCompleted = false;
        try
        {
            IDisposable? roots = NativeResourceRoots524.ReserveSuspensionAckGraph172(complete, runtime, reservations);
            if (roots is null)
            {
                emptyCompleted = true;
                work.CompleteEmptyCapture();

                return new(portable, null, []);
            }
            work.Attach(roots);
            var reserved = reservations.ToArray();
            var graph = new MailboxGraphSnapshot593(runtime, work);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);

            return new(portable, graph, reserved);
        }
        catch (Exception original)
        {
            if (!emptyCompleted)
            {
                work.ReleaseOwner();
                try
                {
                    MailboxRootSettlement593.Settle(work);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(original, cleanup);
                }
            }
            throw;
        }
    }

    // Stored function identity is metadata, not an environment child in the
    // normal resource walkers. Retain its actual disclosed roots too; never
    // invoke a lazy function identity provider or infer authority from a DTO.
    private static Term SuspensionAckMetadataRoots172(Term source)
    {
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        var identities = new List<Term>();
        pending.Push(source);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            if (value is FunctionTerm { StoredLocalIdentity: { } identity })
            {
                identities.Add(identity.Unique);
                pending.Push(identity.Unique);
            }
            foreach (Term child in NativeMapOwnedRoots510.Children(value)) pending.Push(child);
        }

        return identities.Count == 0 ? source : Term.Tuple([source, .. identities]);
    }
}
