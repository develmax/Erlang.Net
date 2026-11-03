namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void StartActualWorkersBeforeInitialization()
    {
        if (System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(RuntimeWorkerStartupNames.RuntimeGate);
        actorDispatcher.StartNormalWorkers();
        // The constructor still owns this unpublished runtime. Keep the owner
        // before starting any worker so partial startup always has a close path.
        dirtyNative419 = new(dirtyIoSchedulerCount522, Statistics.Microstates, workerCreation);
        dirtyNative419.EnsureWallTimeReady(CancellationToken.None).GetAwaiter().GetResult();
    }
}
