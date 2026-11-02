namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Pinned ERTS_PROC_IN_DIRTY_STATE includes scheduled work but excludes a
    // real normal/system turn. Keep physical-only query423 entirely unchanged.
    private bool InDirtyState428(ProcessContext context)
        => dirtyNative419?.HasDirtyWork428(context) == true && !actorDispatcher.IsNormalTurnRunning428(context);

    internal Term CheckDirtyProcessCode428(ProcessContext caller, Term targetTerm, Term moduleTerm)
    {
        RuntimeActorControlLease428? main = null;
        try
        {
            lock (gate)
            {
                RequireDirtyHandlerRole428(caller);
                if (targetTerm is not Pid target || !IsCurrentLocal(target) || moduleTerm is not Atom module)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                if (target.Equals(caller.Self)) return Term.A(SystemTaskNames428.Normal);
                if (!processes.TryGetValue(target, out var context) || context.Terminated) return CoreModules.Boolean(false);
                if (!InDirtyState428(context)) return Term.A(SystemTaskNames428.Normal);
                if (!actorDispatcher.TryAcquireControl428(context, out main)) return Term.A(SystemTaskNames428.Busy);

                // Same real image proof and established runtime->registry lock order
                // as the existing ordered ProcessRuntime.CodeChecks implementation.
                return CoreModules.Boolean(!context.Terminated && Modules.IsExecutingOldCode(context, module.Name));
            }
        }
        finally
        {
            main?.Dispose();
        }
    }

    internal Term HandleDirtySignals428(ProcessContext caller, Term targetTerm, out int workUnits)
    {
        workUnits = 0;
        RuntimeActorControlLease428? main = null;
        ProcessSignalOwnerLease428? signalOwner = null;
        ProcessContext target;
        try
        {
            lock (gate)
            {
                RequireDirtyHandlerRole428(caller);
                if (targetTerm is not Pid pid || !IsCurrentLocal(pid)) return CoreModules.Boolean(false);
                dirtySignalNotifications428.Remove((pid, caller.SchedulingPriorityLevel));
                if (!processes.TryGetValue(pid, out target!) || target.Terminated) return Term.A(SystemTaskNames428.NoProcess);
                if (!InDirtyState428(target)) return Term.A(SystemTaskNames428.Normal);
                if (!target.Mailbox.HasPendingSignals428) return Term.A(CommonAtomNames.Ok);
                if (!actorDispatcher.TryAcquireControl428(target, out main))
                    return Term.A(InDirtyState428(target) ? SystemTaskNames428.More : SystemTaskNames428.Normal);
                if (target.Terminated) return Term.A(SystemTaskNames428.NoProcess);
                if (!InDirtyState428(target)) return Term.A(SystemTaskNames428.Normal);
                signalOwner = TryBeginSignalOwner428(target, true);
                if (signalOwner is null) return Term.A(SystemTaskNames428.Busy);
            }
            int used = 0;
            int budget = Math.Min(SystemTaskNames428.SignalBudget, caller.RemainingSystemTaskReductions428);
            while (used < budget)
            {
                MailboxSignalBatch428 batch;
                MailboxDeferredPreparation? preparation = null;
                MailboxPendingBackingClaim? pendingClaim = null;
                lock (gate)
                {
                    if (target.Terminated) return Term.A(CommonAtomNames.Ok);
                    batch = target.Mailbox.AdmitPendingBounded428(budget - used);
                    used += batch.Processed;
                    workUnits = used;
                    if (!batch.Remaining) return Term.A(CommonAtomNames.Ok);
                    if (batch.PendingBacking) pendingClaim = target.Mailbox.ClaimPendingBacking();
                    else if (batch.Deferred) preparation = target.Mailbox.ClaimDeferredBounded428();
                }
                if (batch.PendingBacking)
                {
                    if (pendingClaim is null) return Term.A(SystemTaskNames428.Busy);
                    var preparedBatch = PrepareAndAdmitPendingMailboxBacking(target, pendingClaim, budget - used);
                    used += preparedBatch.Processed;
                    workUnits = used;
                    continue;
                }
                if (!batch.Deferred) continue;
                if (preparation is null) return Term.A(SystemTaskNames428.Busy);
                DeferredMailboxAdmission? ready;
                try
                {
                    ready = preparation.Prepare();
                }
                catch
                {
                    lock (gate) preparation.Complete(null);
                    throw;
                }
                lock (gate) preparation.Complete(target.Terminated ? null : ready);
                used++;
                workUnits = used;
            }
            lock (gate) return Term.A(target.Terminated || !target.Mailbox.HasPendingSignals428
                ? CommonAtomNames.Ok : SystemTaskNames428.More);
        }
        finally
        {
            signalOwner?.Dispose();
            main?.Dispose();
        }
    }
}
