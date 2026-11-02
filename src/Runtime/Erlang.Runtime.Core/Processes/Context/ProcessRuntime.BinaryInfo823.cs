namespace Erlang;

public sealed partial class ProcessRuntime
{
    private Term? CaptureLiveBinaryInfo823(ProcessContext observed)
    {
        if (!Live()) return null;
        try
        {
            return observed.CaptureBinaryInfo823();
        }
        catch (ObjectDisposedException)
        {
            // Preserve death precedence, without hiding a live heap failure.
            if (!Live()) return null;
            throw;
        }

        bool Live()
        {
            lock (gate)
                return !observed.Terminated && processes.TryGetValue(observed.Self, out var current)
                    && ReferenceEquals(current, observed);
        }
    }

    private void QueueBinaryInfo823(ProcessContext owner, ProcessInfoRequest request)
    {
        if (!System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(ProcessBinaryInfo823Names.InvalidAdmission);
        var admission = new PendingBinaryInfoAdmission823(this, owner, request);
        owner.DeferredSignalDrain ??= new();
        if (!owner.Mailbox.TrySendDeferred606(
            admission.Prepare,
            admission.Release,
            null,
            null
        ))
        {
            owner.PendingInfoRequests.Remove(request);
            ReplyProcessInfo(request, ProcessInfoAtoms.Undefined, owner.Self);
        }
    }

    internal DeferredMailboxAdmission? PrepareBinaryInfo823(PendingBinaryInfoAdmission823 admission)
    {
        RequireBinaryInfoCapture822();
        ProcessContext owner = admission.Owner;
        ProcessInfoRequest request = admission.Request;
        lock (gate)
        {
            if (!ReferenceEquals(admission.Runtime, this))
                throw new InvalidOperationException(ProcessBinaryInfo823Names.InvalidAdmission);
            if (!Current()) return null;
        }
        Term? binary = null;
        Exception? failure = null;
        try
        {
            // Only local scalar data escapes H. No request snapshot/graph is
            // touched while cancellation or termination can win under R.
            binary = owner.CaptureBinaryInfo823();
        }
        catch (Exception error)
        {
            failure = error;
        }
        lock (gate)
        {
            if (!Current()) return null;
            owner.PendingInfoRequests.Remove(request);
            if (failure is not null) request.Snapshot601.Failure = failure;
            else FillProcessInfoSnapshot601(
                request.Snapshot601,
                owner.Self,
                request.Keys,
                request.Singleton,
                binary
            );
            PublishProcessInfoReply601(request, owner.Self);
        }

        // Existing deferred completion removes the actual ordering marker.
        return null;

        bool Current() => !owner.Terminated && !IsHaltRequested
            && processes.TryGetValue(owner.Self, out var actual) && ReferenceEquals(actual, owner)
            && owner.PendingInfoRequests.Contains(request);
    }
}
