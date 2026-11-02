namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void Terminate(
        ProcessContext ctx,
        Term reason,
        RuntimeProcessPayload641? reason641,
        Exception? sourceTeardown641,
        PreparedSequenceTermination? sequence641
    )
    {
        lock (gate)
        {
            if (!processes.TryGetValue(ctx.Self, out ProcessContext? current) || !ReferenceEquals(current, ctx)) return;
            if (sequence641?.ShutdownBatch is { } shutdown && !ReferenceEquals(pendingShutdownSequence, shutdown)
                && !AcceptSequenceShutdown(shutdown, afterInitialAdmission: true))
                throw new InvalidOperationException(SequenceTerminationNames.Unprepared);
            if (sequence641 is null && NeedsSequenceTerminationPreparation(ctx)
                || sequence641 is not null && !sequence641.IsCurrentBeforeTermination())
                throw new InvalidOperationException(SequenceTerminationNames.Unprepared);
            if (!processes.Remove(ctx.Self)) return;
            sequence641?.TransferToRuntime();
            reason = sequence641?.Reason ?? reason;
            reason641 = sequence641?.Reason641 ?? reason641;
            var failures = new List<Exception>();
            if (sourceTeardown641 is not null) failures.Add(sourceTeardown641);
            processSlots.Remove(ctx.Self);
            ctx.Terminated = true;
            if (!ctx.ExecutionStarted) ctx.VmSpawnInput641?.CloseClaimed641();
            ctx.ExitReason = reason;
            CleanupStep(() => RetireObserverMonitors641(ctx));
            if (ctx.Self.Equals(InitializationPid) && pendingShutdownSequence is null)
                CleanupStep(StartNifShutdown);
            IReadOnlyList<DistributionExitOutput> remoteOutput = Array.Empty<DistributionExitOutput>();
            CleanupStep(() => remoteOutput = CaptureDistributionExitOutput(ctx, reason));
            Task storage = Task.CompletedTask;
            Task physical = Task.CompletedTask;
            CleanupStep(() => storage = etsFoundation?.ProcessTerminated(ctx.Self) ?? Task.CompletedTask);
            CleanupStep(() => storage = RegexProcessRetirement411(ctx, storage));
            CleanupStep(() => storage = PersistentProcessRetirement417(ctx, storage));
            CleanupStep(() => storage = SystemTaskProcessRetirement428(ctx, storage));
            CleanupStep(() => physical = DirtyNativeProcessRetirement419(ctx, Task.CompletedTask));
            CleanupStep(() => physical = PublicWallTimeProcessRetirement(ctx, physical));
            CleanupStep(() => storage = DebugDispatchProcessRetirement432(ctx, storage));
            CleanupStep(() => storage = CountersProcessRetirement421(ctx, storage));
            CleanupStep(() => storage = HamtProcessRetirement429(ctx, storage));
            CleanupStep(() => storage = EtsQueueRetirement419(ctx, storage));
            CleanupStep(() => storage = DriverFrontendProcessRetirement422(ctx, storage, reason641));
            CleanupStep(() => storage = CodePurgerProcessRetirement432(ctx, storage));
            CleanupStep(() => physical = AllocatorGatherProcessRetirement434(ctx, physical));
            CleanupStep(() => physical = ProcessPayloadRetirement641(ctx, physical));
            CleanupStep(() => physical = OwnedTermProcessRetirement506(ctx, physical));
            if (sequence641 is not null) physical = Task.WhenAll(physical, sequence641.FanoutRetired.Task);
            ProcessExitContinuation? exit = null;
            if (!storage.IsCompleted)
            {
                exit = new(ctx, reason, remoteOutput)
                {
                    StorageRetirement = storage,
                    PhysicalRetirement = physical,
                    PreparationFailures = failures,
                    Sequence641 = sequence641
                };
                exit.Origin641 = reason641?.Borrow641(null);
                StartExitContinuation(exit);
                // Stop the actor before retirement; its finally may release a read lease.
                CleanupStep(ctx.Stop.Cancel);
                if (ctx.Self.Equals(InitializationPid) && pendingShutdownSequence is not null && !sequenceLogicalShutdownCommitted)
                    CommitSequenceLogicalShutdown();

                return;
            }
            CleanupStep(() => storage.GetAwaiter().GetResult());
            if (remoteOutput.Count != 0 || !physical.IsCompletedSuccessfully || sequence641 is not null)
            {
                exit = new(ctx, reason, remoteOutput) { PhysicalRetirement = physical, Sequence641 = sequence641 };
                exit.Origin641 = reason641?.Borrow641(null);
                StartExitContinuation(exit);
            }
            FinishTermination(
                ctx,
                reason,
                failures,
                exit,
                reason641,
                sequence641
            );
            if (ctx.Self.Equals(InitializationPid) && pendingShutdownSequence is not null && !sequenceLogicalShutdownCommitted)
                CommitSequenceLogicalShutdown();

            void CleanupStep(Action cleanup)
            {
                try
                {
                    cleanup();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
        }
    }

    private void FinishTermination(
        ProcessContext ctx,
        Term reason,
        List<Exception> failures,
        ProcessExitContinuation? exit,
        RuntimeProcessPayload641? reason641,
        PreparedSequenceTermination? sequence641
    )
    {
        CleanupStep(() => DirtySystemHandlerEnded428(ctx));
        CleanupStep(() => CollectorProcessEnded413(ctx));
        CleanupStep(() => LocalNodeProcessTerminated(ctx, reason));
        CleanupStep(() => distributionPending?.ProcessTerminated(ctx, reason, reason641));
        CleanupStep(() => ProcessTracing.Terminated(ctx.Self));
        CleanupStep(() => SystemMonitoring.Terminated(ctx.Self));
        CleanupStep(() => SystemProfiling.Terminated(ctx.Self));
        CleanupStep(() => FinishProcessAlive425(ctx));
        CleanupStep(() => FinishInternalGroupLeader425(ctx));
        foreach (var request in ctx.PendingCodeChecks)
            CleanupStep(() => ReplyCodeCheck(request, false, ctx.Self));
        ctx.PendingCodeChecks.Clear();
        foreach (var request in ctx.PendingInfoRequests)
            CleanupStep(() => ReplyProcessInfo(request, ProcessInfoAtoms.Undefined, ctx.Self));
        ctx.PendingInfoRequests.Clear();
        foreach (var request in ctx.PendingDisplayRequests)
            CleanupStep(() => ReplyProcessDisplay(request, Term.A(ErlangErrorReasons.BadArgument)));
        ctx.PendingDisplayRequests.Clear();
        CleanupStep(() => RetireCollectionRequests(ctx, true));
        foreach (var request in ctx.PendingSavedCallRequests)
            CleanupStep(() => ReplySavedCallRequest(request, Term.A(ErlangErrorReasons.BadArgument), ctx.Self));
        ctx.PendingSavedCallRequests.Clear();
        CleanupStep(() => Timers.RecipientTerminated(ctx.Self));
        CleanupStep(() => Suspensions.Terminated(ctx));
        CleanupStep(ctx.Mailbox.DiscardPending);
        foreach (var request in spawnRequests.Where(pair => pair.Value.Owner.Equals(ctx.Self)).Select(pair => pair.Key).ToArray())
            spawnRequests.Remove(request);
        foreach (var alias in aliases.Where(pair => pair.Value.Owner.Equals(ctx.Self)).Select(pair => pair.Key).ToArray())
            aliases.Remove(alias);
        CleanupStep(() => Ports.OwnerTerminated641(ctx, reason, reason641));
        foreach (var name in names.Where(pair => pair.Value.Equals(ctx.Self)).Select(pair => pair.Key).ToArray())
            names.Remove(name);
        var linkedExits = sequence641 is null
            ? ctx.Links.Select(pid => (Pid: pid, Reason: reason))
                .Concat(ctx.PendingLinkOrigins.Values.Select(pid => (Pid: pid, Reason: (Term)Term.A(ErlangErrorReasons.NoProcess)))).ToArray()
            : Enumerable.Range(0, sequence641.Snapshot.Count)
                .Select(index => (Pid: sequence641.Snapshot.Destination(index), Reason: sequence641.ReasonFor(index))).ToArray();
        ctx.PendingLinkOrigins.Clear();
        ctx.Links.Clear();
        ctx.LinkIdentities.Clear();
        ctx.PriorityLinks.Clear();
        foreach (var monitor in monitors.ToArray())
        {
            if (monitor.Value.Target?.Equals(ctx.Self) == true)
            {
                if (processes.TryGetValue(monitor.Value.Observer, out var observer))
                    CleanupStep(() => MonitorDown(
                        observer,
                        monitor.Key,
                        monitor.Value,
                        reason,
                        reason641
                    ));
            }
            else if (monitor.Value.Observer.Equals(ctx.Self))
                RemoveMonitor641(monitor.Key);
        }
        CleanupStep(ctx.Stop.Cancel);
        foreach (var linkedExit in linkedExits)
        {
            CleanupStep(() =>
            {
                PreparedMailboxMessage? signal = null;
                SequenceTraceToken? exitToken = sequence641 is null ? null
                    : SequenceTracing.LinkedExitPrepared(
                        ctx,
                        linkedExit.Pid,
                        linkedExit.Reason,
                        sequence641,
                        out signal
                    );
                if (processes.TryGetValue(linkedExit.Pid, out var linked))
                {
                    RuntimeProcessPayload641? linkedReason641 = ReferenceEquals(linkedExit.Reason, reason) ? reason641 : null;
                    var fanout641 = RuntimeProcessPayload641.CombinePublication641(
                        this,
                        null,
                        Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), ctx.Self, linkedExit.Reason),
                        linkedReason641,
                        sequence641?.Token641?.Payload641
                    );
                    try
                    {
                        QueueExitWithSequenceControl(
                            linked,
                            ctx.Self,
                            linkedExit.Reason,
                            false,
                            false,
                            false,
                            null,
                            true,
                            exitToken,
                            fanout641,
                            signal
                        );
                    }
                    finally
                    {
                        fanout641.Close641();
                    }
                }
            });
        }
        if (failures.Count == 0)
        {
            if (exit is not null)
                exit.LocalCleanup.TrySetResult();
            else
                ctx.Done.TrySetResult(reason);
        }
        else
        {
            var failure = new AggregateException(failures);
            if (exit is not null)
                exit.LocalCleanup.TrySetException(failure);
            else
            {
                exitContinuationFailures.Add(failure);
                ctx.Done.TrySetException(failure);
            }
            throw failure;
        }

        void CleanupStep(Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
    }

}
