namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool? AdmitAlias817(PendingAliasAdmission817 admission)
    {
        if (!System.Threading.Monitor.IsEntered(gate) || !ReferenceEquals(admission.Runtime, this))
            throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        if (!aliases.TryGetValue(admission.Reference, out var current)) return null;
        if (current.Reply)
        {
            aliases.Remove(admission.Reference);
            RemoveMonitor641(admission.Reference);
            DetachConnectedSpawnMonitor(admission.Owner, admission.Reference);
        }

        return admission.Priority && current.Priority;
    }

    internal bool? AdmitProcessInfo817(PendingProcessInfoAdmission817 admission)
    {
        if (!System.Threading.Monitor.IsEntered(gate) || !ReferenceEquals(admission.Runtime, this))
            throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        lock (gate)
            if (admission.Owner.PendingInfoRequests.Remove(admission.Request))
            {
                FillProcessInfoSnapshot601(
                    admission.Request.Snapshot601,
                    admission.Target,
                    admission.Request.Keys,
                    admission.Request.Singleton
                );
                PublishProcessInfoReply601(admission.Request, admission.Owner.Self);
            }

        return null;
    }

    internal bool IsReadonlyAliasAdmission817(PendingAliasAdmission817 admission, ProcessContext owner)
        => System.Threading.Monitor.IsEntered(gate) && ReferenceEquals(admission.Runtime, this) && ReferenceEquals(admission.Owner, owner)
            && !admission.Priority && aliases.TryGetValue(admission.Reference, out var current)
            && current.Owner.Equals(owner.Self) && !current.Reply && !current.Priority;

    internal bool IsReadonlyInfoAdmission817(PendingProcessInfoAdmission817 admission, ProcessContext owner)
        => System.Threading.Monitor.IsEntered(gate) && ReferenceEquals(admission.Runtime, this) && ReferenceEquals(admission.Owner, owner)
            && admission.Target.Equals(owner.Self)
            && admission.Request.Keys.All(key => key.Name is ProcessInfoNames.MessageQueueLength or ProcessInfoNames.Status)
            && admission.Request.Requester.SequentialTrace.Token is null
            && ProcessTracing.HasNoOutputForReadonlyInfo817(admission.Request.Requester);
}
