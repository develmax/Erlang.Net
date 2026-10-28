namespace Erlang.NifNativeBackend;

internal static class RuntimeQueueStatistics504HostChecks
{
    internal static void HeldDirtyQueues(ProcessContext context)
    {
        var owner = new DirtyNativeOwner419();
        using var cpuEntered = new ManualResetEventSlim();
        using var ioEntered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var outcomes = new List<Task<Term>>();
        try
        {
            Task<Term> Enqueue(DirtyNativeKind419 kind, ManualResetEventSlim? entered) => owner.Enqueue(
                context,
                kind,
                RuntimeQueueStatistics504HostNames.Module,
                RuntimeQueueStatistics504HostNames.Function,
                [],
                (_, _) =>
                {
                    entered?.Set();
                    Require(release.Wait(RuntimeQueueStatistics504HostNames.Timeout));

                    return Nil.Value;
                }
            );
            outcomes.Add(Enqueue(DirtyNativeKind419.Cpu, cpuEntered));
            outcomes.Add(Enqueue(DirtyNativeKind419.Io, ioEntered));
            Require(cpuEntered.Wait(RuntimeQueueStatistics504HostNames.Timeout));
            Require(ioEntered.Wait(RuntimeQueueStatistics504HostNames.Timeout));
            outcomes.Add(Enqueue(DirtyNativeKind419.Cpu, null));
            outcomes.Add(Enqueue(DirtyNativeKind419.Io, null));
            Require(owner.QueueLengths504(false) == (1, 1));
            Require(owner.QueueLengths504(true) == (2, 2));

            var queued = owner.QueueLengths504(false);
            var active = owner.QueueLengths504(true);
            long[] normals = [3, 5];
            Check(
                RuntimeStatisticsNames.RunQueue,
                normals,
                queued,
                Term.I(9)
            );
            Check(
                RuntimeStatisticsNames.TotalRunQueueLengths,
                normals,
                queued,
                Term.I(9)
            );
            Check(
                RuntimeStatisticsNames.TotalRunQueueLengthsAll,
                normals,
                queued,
                Term.I(10)
            );
            Check(
                RuntimeStatisticsNames.TotalActiveTasks,
                normals,
                active,
                Term.I(10)
            );
            Check(
                RuntimeStatisticsNames.TotalActiveTasksAll,
                normals,
                active,
                Term.I(12)
            );
            Check(
                RuntimeStatisticsNames.RunQueueLengths,
                normals,
                queued,
                Cons.From([Term.I(3), Term.I(5), Term.I(1)])
            );
            Check(
                RuntimeStatisticsNames.RunQueueLengthsAll,
                normals,
                queued,
                Cons.From([Term.I(3), Term.I(5), Term.I(1), Term.I(1)])
            );
            Check(
                RuntimeStatisticsNames.ActiveTasks,
                normals,
                active,
                Cons.From([Term.I(3), Term.I(5), Term.I(2)])
            );
            Check(
                RuntimeStatisticsNames.ActiveTasksAll,
                normals,
                active,
                Cons.From([Term.I(3), Term.I(5), Term.I(2), Term.I(2)])
            );
            Check(
                RuntimeStatisticsNames.RunQueues,
                normals,
                queued,
                Term.Tuple(
                    Term.I(3),
                    Term.I(5),
                    Term.I(1),
                    Term.I(1)
                )
            );

            release.Set();
            Task.WhenAll(outcomes).WaitAsync(RuntimeQueueStatistics504HostNames.Timeout).GetAwaiter().GetResult();
            owner.BeginClose().WaitAsync(RuntimeQueueStatistics504HostNames.Timeout).GetAwaiter().GetResult();
            Require(owner.QueueLengths504(false) == (0, 0));
            Require(owner.QueueLengths504(true) == (0, 0));
        }
        finally
        {
            release.Set();
            // Join actual finite bodies before disposing their synchronization state.
            owner.BeginClose().GetAwaiter().GetResult();
        }
    }

    private static void Check(
        string key,
        long[] normal,
        (long Cpu, long Io) dirty,
        Term expected
    )
    {
        Require(RuntimeSchedulerStatistics.IsQueue(key));
        Require(RuntimeSchedulerStatistics.Format(
            key,
            normal,
            dirty.Cpu,
            dirty.Io
        ).Equals(expected));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(RuntimeQueueStatistics504HostNames.Failure);
    }
}
