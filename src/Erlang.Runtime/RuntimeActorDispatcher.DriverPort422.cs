namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private int nextDriverPortLane422;
    private int driverOwners422;
    private bool[]? driverAuxiliaryYield422;

    internal DriverDispatcherLease422 AcquireDriverOwner422()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            driverAuxiliaryYield422 ??= new bool[lanes.Length];
            var lease = new DriverDispatcherLease422(this);
            driverOwners422 = checked(driverOwners422 + 1);

            return lease;
        }
    }

    internal void ReleaseDriverOwner422()
    {
        lock (gate)
        {
            if (driverOwners422 <= 0) throw new InvalidOperationException(DriverParentWireNames422.Invalid);
            driverOwners422--;
            if (driverOwners422 == 0) driverAuxiliaryYield422 = null;
            CompleteIfIdle();
        }
    }

    private bool DriverAuxiliaryMayRun422(RuntimeSchedulerLane lane)
    {
        if (driverAuxiliaryYield422 is null || !driverAuxiliaryYield422[lane.Identity - ProcessSchedulerNames.First]) return true;
        bool guestReady = lane.Online && (lane.Maximum.Count != 0 || lane.High.Count != 0 || lane.Ordinary.Count != 0)
            || retiring && lane.Cleanup.Count != 0;

        return !guestReady;
    }

    private void DriverAuxiliarySelected422(RuntimeSchedulerLane lane, bool auxiliary)
    {
        if (driverAuxiliaryYield422 is not null)
            driverAuxiliaryYield422[lane.Identity - ProcessSchedulerNames.First] = auxiliary;
    }

    internal Task DispatchDriverPort422(ProcessRuntime runtime, DriverDispatcherLease422 owner, Func<Task> callback)
    {
        var job = new DriverPortDispatchJob422(runtime, callback);
        var work = new RuntimeSchedulerAuxiliary420(job.Execute);
        lock (gate)
        {
            try
            {
                if (!owner.IsOwnedBy(this)) throw new ObjectDisposedException(nameof(DriverDispatcherLease422));
                RuntimeSchedulerLane? chosen = null;
                for (int index = 0; index < lanes.Length; index++)
                {
                    if (nextDriverPortLane422 == lanes.Length) nextDriverPortLane422 = 0;
                    RuntimeSchedulerLane candidate = lanes[nextDriverPortLane422++];
                    if (candidate.Online)
                    {
                        chosen = candidate;
                        break;
                    }
                }
                if (chosen is null) throw new InvalidOperationException(DriverPortDispatchNames422.NoOnlineLane);
                chosen.Auxiliary420.AddLast(work.Node);
                StartWorkers();
            }
            catch (Exception failure)
            {
                work.Node.List?.Remove(work.Node);
                job.Abandon();
                work.Fail(failure);
            }
        }

        return work.Completion;
    }
}
