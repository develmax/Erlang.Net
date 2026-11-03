namespace Erlang;

public sealed partial class ProcessRuntime
{
    private bool sequenceLogicalShutdownCommitted;
    private bool nifShutdownOwnedByPreparedSequence;

    private void CommitSequenceLogicalShutdown()
    {
        if (!System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(SequenceTerminationNames.Unprepared);
        if (sequenceLogicalShutdownCommitted || IsHaltRequested) return;
        sequenceLogicalShutdownCommitted = true;
        StopLocalNode();
        localNodeEvents?.Close();
        disposed = true;
        StopMailboxRootAdmission593();
        StopProcessPayloadAdmission641();
        StopOrdinaryBinaryAdmission();
        _ = actorDispatcher.CompleteWhenIdle();
        Timers.Dispose();
        distributionPending?.Close();
        OsEnvironment?.Dispose();
        CloseDistributionEntryIdentities();
        etsFoundation?.Close();
        _ = ClosePersistentTerms();
        _ = CloseSystemTasks428();
        _ = CloseDirtyNative419();
        _ = CloseDebugDispatch432();
        _ = CloseCounters421();
        _ = CloseHamtIndex429();
        _ = CloseRegex411();
        _ = CloseBinaryPrograms598();
        _ = CloseSavedMatchPrograms634();
        _ = CloseDriverFrontend422();
        _ = CloseAtomics413();
        CloseCollectors413();
        _ = CloseCodePurger432();
        ProcessTracing.StopPatternRoots778();
        ProcessTracing.CloseTraceSessions425();
        _ = CloseNativeResources524();
        ReferenceResources.Close();
        Ports.Shutdown();
        foreach (var context in processes.Values.ToArray())
        {
            PreparedSequenceTermination? plan = ProjectShutdownSequence(context);
            Terminate(
                context,
                Term.A(ProcessExitReasons.Shutdown),
                null,
                null,
                plan
            );
        }
        if (!nifShutdownOwnedByPreparedSequence) StartNifShutdown();
    }

    private Task? BeginSequenceDisposeCore()
    {
        Task? haltWait = haltResult is null ? null : haltQuiescence.Task;
        if (haltWait is null) CommitSequenceLogicalShutdown();

        return haltWait;
    }
}
