namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly Dictionary<ProcessContext, ProcessSignalOwnerLease428> signalOwners428 = new();

    private ProcessSignalOwnerLease428? TryBeginSignalOwner428(ProcessContext target, bool dirtyHandler = false)
    {
        lock (gate)
        {
            if (!ReferenceEquals(target.Runtime, this)) throw new ArgumentException(RuntimeActorControlNames428.Foreign, nameof(target));
            if (signalOwners428.ContainsKey(target)) return null;
            var owner = new ProcessSignalOwnerLease428(this, target, dirtyHandler);
            signalOwners428.Add(target, owner);

            return owner;
        }
    }

    internal void ReleaseSignalOwner428(ProcessContext target, ProcessSignalOwnerLease428 lease)
    {
        lock (gate)
        {
            if (signalOwners428.TryGetValue(target, out var found) && ReferenceEquals(found, lease))
                signalOwners428.Remove(target);
            if (!lease.DirtyHandler) NotifyDirtySignalHandler428(target, null);
        }
    }
}
