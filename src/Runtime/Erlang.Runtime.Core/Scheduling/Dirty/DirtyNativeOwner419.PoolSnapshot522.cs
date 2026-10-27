namespace Erlang;

internal sealed partial class DirtyNativeOwner419
{
    internal DirtyNativePoolSnapshot522 PoolSnapshot522()
    {
        lock (gate)
            return new(
                closed,
                workers522.Count(worker => worker.Kind == DirtyNativeKind419.Cpu),
                ioWorkers522.Length,
                invocations.Count(call => !call.Running),
                invocations.Count(call => call.Running),
                Completion.IsCompletedSuccessfully,
                Array.AsReadOnly(workers522.Select(worker => new DirtyNativeWorkerSnapshot522(
                    worker.Kind,
                    worker.ThreadId,
                    worker.Alive,
                    worker.Quiesced.Task.IsCompletedSuccessfully,
                    worker.Joined522
                )).ToArray())
            );
    }
}
