namespace Erlang;

public sealed partial class Mailbox
{
    internal void EmitReadonlyAdmission817(Term value, Term? from) => MessageAdmitted?.Invoke(value, from);

    // Only metadata/capability acquisition while the genuine source Entry lives.
    internal ReadonlyTracePrefix817? CaptureReadonlyPrefix817(ProcessContext receiver)
    {
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || policyMoveReady || pendingSignals428.Count == 0) return null;
            var nodes = new List<ReadonlyTracePrefixNode817>();
            bool anyAlias = false;
            try
            {
                foreach (var index in pendingSignals428)
                {
                    var node = index;
                    Entry entry = node.Value;
                    if (entry.PendingBacking || entry.Deferred || entry.TraceToken is not null
                        || entry.DistributionTraceToken is not (null or Nil)) return Abort();
                    if (entry.Admission?.Target is PendingAliasAdmission817 alias
                        && ReferenceEquals(entry.Admission, alias.Callback)
                        && receiver.Runtime.TryGetReadonlyAliasPriority817(alias, receiver, out bool priority)
                        && entry.TraceAdmission && entry.Custody593 is { } custody)
                    {
                        var source = ReadonlyTraceSource817.CaptureEntry(receiver, custody, entry.Value);
                        if (source is null) return Abort();
                        try
                        {
                            nodes.Add(new(node, source, priority));
                        }
                        catch
                        {
                            source.Dispose();
                            throw;
                        }
                        anyAlias = true;
                    }
                    else if (entry.Admission?.Target is PendingProcessInfoAdmission817 info
                        && ReferenceEquals(entry.Admission, info.Callback)
                        && receiver.Runtime.IsReadonlyInfoAdmission817(info, receiver)) nodes.Add(new(node, null));
                    else return Abort();
                }

                return anyAlias ? new(nodes, mutationRevision, MessageQueuePolicyRevision) : Abort();
            }
            catch (Exception primary)
            {
                ReadonlyTracePrefix817.ReleaseSourceOperations817(nodes, primary);
                throw;
            }

            ReadonlyTracePrefix817? Abort()
            {
                // Only operation metadata. Actual source work cannot close while
                // the original Entry owner still lives; native teardown is queued.
                ReadonlyTracePrefix817.ReleaseSourceOperations817(nodes, null);

                return null;
            }
        }
    }

    internal bool IsCurrentReadonlyPrefix817(ProcessContext receiver, ReadonlyTracePrefix817 prefix)
    {
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || policyMoveReady || mutationRevision != prefix.Revision
                || MessageQueuePolicyRevision != prefix.PolicyRevision || pendingSignals428.Count != prefix.Nodes.Count) return false;
            foreach (var item in prefix.Nodes)
            {
                if (!ReferenceEquals(item.Node.List, messages)
                    || !ReferenceEquals(item.Node.Value.Value, item.Before.Value)
                    || !ReferenceEquals(item.Node.Value.Admission, item.Before.Admission)
                    || !ReferenceEquals(item.Node.Value.Custody593, item.Before.Custody593)) return false;
                if (item.Before.Admission?.Target is PendingAliasAdmission817 alias)
                {
                    if (!receiver.Runtime.TryGetReadonlyAliasPriority817(alias, receiver, out bool priority)
                        || priority != item.Priority817) return false;
                }
                else if (item.Before.Admission?.Target is PendingProcessInfoAdmission817 info)
                {
                    if (!receiver.Runtime.IsReadonlyInfoAdmission817(info, receiver)) return false;
                }
                else return false;
            }

            return true;
        }
    }

    internal void AdmitReadonlyPrefix817(ProcessTracing tracing, ReadonlyTracePrefix817 prefix)
    {
        foreach (var item in prefix.Nodes)
        {
            bool notify = false, promote = false;
            lock (gate)
            {
                if (Volatile.Read(ref closed593) || policyMoveReady) return;
                var node = item.Node;
                if (!ReferenceEquals(node.List, messages) || node.Value.Admission is not { } admission)
                    throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
                RemovePendingSignal428(node);
                pending--;
                mutationRevision++;
                precedenceRevision++;
                node.Value = node.Value with { Admission = null, Control = true };
                bool? priority = admission();
                if (Volatile.Read(ref closed593)) return;
                if (priority is null) RemoveNode(node);
                else
                {
                    if (priority.Value != item.Priority817) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
                    promote = priority.Value;
                    if (!promote)
                    {
                        node.Value = node.Value with { Control = false };
                        admittedCount++;
                        ObserveQueueLength419(admittedCount);
                        if (Volatile.Read(ref closed593)) return;
                    }
                    notify = node.Value.TraceAdmission;
                    if (notify) item.BindAccepted817(node.Value);
                }
            }
            // PIN traces a priority message BEFORE linking it into the priority
            // queue/incrementing mq_len. Same R, but no M/guest getter/import.
            if (notify) item.Emit(tracing, this);
            if (promote)
            {
                lock (gate)
                {
                    if (Volatile.Read(ref closed593)) return;
                    var node = item.Node;
                    if (!ReferenceEquals(node.List, messages) || !node.Value.Control || item.Source is null)
                        throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
                    receiveMarkerPriorityRevision++;
                    BeforeBeamPriorityInsertReadonly817(item.Source);
                    BeforeBeamSaveUnlink(node);
                    messages.Remove(node);
                    if (lastPriority is null) messages.AddFirst(node); else messages.AddAfter(lastPriority, node);
                    lastPriority = node;
                    node.Value = node.Value with { Control = false };
                    admittedCount++;
                    ObserveQueueLength419(admittedCount);
                    if (Volatile.Read(ref closed593)) return;
                }
            }
        }
    }
}
