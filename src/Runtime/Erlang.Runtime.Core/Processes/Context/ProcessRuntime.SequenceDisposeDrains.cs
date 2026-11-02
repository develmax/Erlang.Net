namespace Erlang;

public sealed partial class ProcessRuntime
{
    private async ValueTask FinishSequenceDisposeDrains(Task? haltWait)
    {
        if (haltWait is not null)
        {
            await haltWait.ConfigureAwait(false);

            return;
        }
        await initializer.Completion.ConfigureAwait(false);
        await Ports.Drain().ConfigureAwait(false);
        await Task.WhenAll(
            distributionPending?.DrainOwnedOutputOperations() ?? Task.CompletedTask,
            Timers.StopForHaltAsync(),
            DrainExitContinuations(),
            DrainEtsRetirements(),
            DrainPersistentTerms(),
            DrainRegex411(),
            CloseBinaryPrograms598(),
            CloseSavedMatchPrograms634(),
            DrainSystemTasks428(),
            DrainDirtyNative419(),
            DrainDebugDispatch432(),
            DrainCounters421(),
            DrainMailboxRoots593(),
            DrainProcessPayloads641(),
            DrainAtomics586(),
            DrainHamtIndex429(),
            DrainDriverFrontend422(),
            DrainCodePurger432(),
            DrainAllocatorGather434(),
            DrainOwnedTermContexts506()
        ).ConfigureAwait(false);
        if (ownsStandardStreams)
            StandardStreams.Dispose();
    }

}
