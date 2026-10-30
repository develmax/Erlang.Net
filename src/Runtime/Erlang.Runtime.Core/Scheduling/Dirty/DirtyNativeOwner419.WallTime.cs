namespace Erlang;

internal sealed partial class DirtyNativeOwner419
{
    internal Task EnsureWallTimeReady(CancellationToken cancellation)
    {
        Task ready;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, DirtyNativeNames419.Closed);
            StartLocked();
            ready = Task.WhenAll(workers522.Select(worker => worker.Started.Task));
        }

        return ready.WaitAsync(cancellation);
    }

    internal RuntimeDirtyWallTimeSample[] WallTimeSnapshot(bool includeIo)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, DirtyNativeNames419.Closed);
            foreach (DirtyNativeWorker419 worker in workers522)
            {
                worker.Started.Task.GetAwaiter().GetResult();
                if (worker.Quiesced.Task.IsCompleted)
                    throw new InvalidOperationException(nameof(DirtyNativeOwner419));
            }

            return workers522.Where(worker => includeIo || worker.Kind == DirtyNativeKind419.Cpu)
                .Select(worker => worker.SampleWallTime()).ToArray();
        }
    }
}
