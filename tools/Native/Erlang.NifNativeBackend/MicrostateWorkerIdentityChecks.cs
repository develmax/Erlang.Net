using System.Collections.Concurrent;

namespace Erlang.NifNativeBackend;

public static class MicrostateWorkerIdentityChecks
{
    public static async Task NormalOutOfOrder()
    {
        var accounting = new RuntimeMicrostateAccounting();
        using var secondEntered = new ManualResetEventSlim();
        var records = new ConcurrentDictionary<int, RuntimeMicrostateRecord>();
        var failures = new ConcurrentQueue<Exception>();
        var policy = Policy(
            RuntimeWorkerCreationNames.Normal,
            accounting,
            secondEntered,
            records
        );
        var first = new RuntimeNormalWorker(
            1,
            accounting,
            null,
            () => { },
            failures.Enqueue,
            policy
        );
        var second = new RuntimeNormalWorker(
            2,
            accounting,
            null,
            () => { },
            failures.Enqueue,
            policy
        );
        try
        {
            first.Start();
            second.Start();
            await Task.WhenAll(first.Ready, second.Ready).WaitAsync(MicrostateWorkerIdentityNames.Timeout);
            Verify(records, accounting, RuntimeMicrostateNames.Scheduler);
            CpuStatisticsAccess.Require(failures.IsEmpty);
        }
        finally
        {
            secondEntered.Set();
            first.Stop();
            second.Stop();
            await Task.WhenAll(first.JoinAsync(), second.JoinAsync()).WaitAsync(MicrostateWorkerIdentityNames.Timeout);
        }
    }

    public static async Task DirtyOutOfOrder()
    {
        var accounting = new RuntimeMicrostateAccounting();
        using var secondEntered = new ManualResetEventSlim();
        var records = new ConcurrentDictionary<int, RuntimeMicrostateRecord>();
        var policy = Policy(
            RuntimeWorkerCreationNames.DirtyIo,
            accounting,
            secondEntered,
            records
        );
        var owner = new DirtyNativeOwner419(2, accounting, policy);
        try
        {
            await owner.EnsureWallTimeReady(CancellationToken.None).WaitAsync(MicrostateWorkerIdentityNames.Timeout);
            Verify(records, accounting, RuntimeMicrostateNames.DirtyIo);
        }
        finally
        {
            secondEntered.Set();
            await owner.BeginClose().WaitAsync(MicrostateWorkerIdentityNames.Timeout);
        }
    }

    private static RuntimeWorkerCreationPolicy Policy(
        string selectedKind,
        RuntimeMicrostateAccounting accounting,
        ManualResetEventSlim secondEntered,
        ConcurrentDictionary<int, RuntimeMicrostateRecord> records
    )
        => new(
            null,
            (kind, identity) =>
        {
            if (kind != selectedKind) return;
            RuntimeMicrostateRecord record = RuntimeMicrostateExecution.Current!.Record!;
            CpuStatisticsAccess.Require(ReferenceEquals(record.Owner, accounting));
            CpuStatisticsAccess.Require(ReferenceEquals(record.PhysicalThread, RuntimeMicrostateThreadIdentity.Current));
            CpuStatisticsAccess.Require(records.TryAdd(identity, record));
            if (identity == 2)
            {
                CpuStatisticsAccess.Require(!records.ContainsKey(1));
                secondEntered.Set();
            }
        },
            null,
            (kind, identity) =>
        {
            if (kind == selectedKind && identity == 1)
                CpuStatisticsAccess.Require(secondEntered.Wait(MicrostateWorkerIdentityNames.Timeout));
        }
        );

    private static void Verify(ConcurrentDictionary<int, RuntimeMicrostateRecord> records,
        RuntimeMicrostateAccounting accounting, string type)
    {
        CpuStatisticsAccess.Require(records.Count == 2);
        CpuStatisticsAccess.Require(records[1].Identity == 1 && records[2].Identity == 2);
        CpuStatisticsAccess.Require(records.Values.All(record => record.Type == type && ReferenceEquals(record.Owner, accounting)));
        CpuStatisticsAccess.Require(!ReferenceEquals(records[1].PhysicalThread, records[2].PhysicalThread));
    }

    public static async Task DuplicatePhysicalThreadIdentity()
    {
        var accounting = new RuntimeMicrostateAccounting();
        using var firstEntered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var firstDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RuntimeMicrostateRecord? firstRecord = null;
        RuntimeMicrostateThreadIdentity? secondThread = null;
        Exception? secondFailure = null;
        var first = new Thread(() =>
        {
            try
            {
                using var scope = accounting.AttachConfiguredWorker(RuntimeMicrostateNames.Scheduler, 1, RuntimeMicrostate.Sleep);
                firstRecord = RuntimeMicrostateExecution.Current!.Record!;
                using (var repeated = accounting.AttachConfiguredWorker(RuntimeMicrostateNames.Scheduler, 1, RuntimeMicrostate.Other))
                    CpuStatisticsAccess.Require(ReferenceEquals(firstRecord, RuntimeMicrostateExecution.Current!.Record));
                firstEntered.Set();
                CpuStatisticsAccess.Require(release.Wait(MicrostateWorkerIdentityNames.Timeout));
                firstDone.TrySetResult();
            }
            catch (Exception error)
            {
                firstEntered.Set();
                firstDone.TrySetException(error);
            }
        });
        var second = new Thread(() =>
        {
            try
            {
                CpuStatisticsAccess.Require(firstEntered.Wait(MicrostateWorkerIdentityNames.Timeout));
                secondThread = RuntimeMicrostateThreadIdentity.Current;
                using var scope = accounting.AttachConfiguredWorker(RuntimeMicrostateNames.Scheduler, 1, RuntimeMicrostate.Sleep);
            }
            catch (Exception error)
            {
                secondFailure = error;
            }
            finally
            {
                secondDone.TrySetResult();
            }
        });
        bool firstStarted = false, secondStarted = false;
        try
        {
            using (ExecutionContext.SuppressFlow())
            {
                first.Start();
                firstStarted = true;
                second.Start();
                secondStarted = true;
            }
            await secondDone.Task.WaitAsync(MicrostateWorkerIdentityNames.Timeout);
            CpuStatisticsAccess.Require(firstRecord is not null && secondThread is not null);
            CpuStatisticsAccess.Require(!ReferenceEquals(firstRecord!.PhysicalThread, secondThread));
            CpuStatisticsAccess.Require(secondFailure is InvalidOperationException { Message: RuntimeMicrostateNames.ThreadMismatch });
        }
        finally
        {
            release.Set();
            await Task.WhenAll(
                firstStarted ? Task.Run(first.Join) : Task.CompletedTask,
                secondStarted ? Task.Run(second.Join) : Task.CompletedTask
            ).WaitAsync(MicrostateWorkerIdentityNames.Timeout);
        }
        await firstDone.Task.WaitAsync(MicrostateWorkerIdentityNames.Timeout);
    }
}
