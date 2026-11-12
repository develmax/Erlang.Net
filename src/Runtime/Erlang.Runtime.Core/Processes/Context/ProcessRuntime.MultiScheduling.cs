namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly SemaphoreSlim multiSchedulingChange = new(1, 1);
    private readonly Dictionary<ProcessContext, RuntimeMultiSchedulingBlocker> multiSchedulingBlockers = new();
    private bool multiSchedulingDirtyPaused;

    // Caller holds the ordinary runtime gate. A newly created dirty pool must
    // inherit an already-published block before any invocation can be enqueued.
    private DirtyNativeOwner419 GetDirtyNativeOwner419()
    {
        if (dirtyNative419 is null)
        {
            var owner = new DirtyNativeOwner419(dirtyIoSchedulerCount522, Statistics.Microstates);
            if (multiSchedulingDirtyPaused)
            {
                _ = owner.PauseScheduling();
            }
            dirtyNative419 = owner;
        }

        return dirtyNative419;
    }

    internal async ValueTask<Term> ChangeMultiScheduling(ProcessContext context, Term value)
    {
        if (value is not Atom
            {
                Name: RuntimeMultiSchedulingNames.Block or RuntimeMultiSchedulingNames.Unblock
            or RuntimeMultiSchedulingNames.BlockNormal or RuntimeMultiSchedulingNames.UnblockNormal
            } operation
            || !ReferenceEquals(context.Runtime, this) || !RuntimeActorExecutionTurn.IsOwnedBy(context))
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        bool block = operation.Name is RuntimeMultiSchedulingNames.Block or RuntimeMultiSchedulingNames.BlockNormal;
        bool normal = operation.Name is RuntimeMultiSchedulingNames.BlockNormal or RuntimeMultiSchedulingNames.UnblockNormal;
        Task permit = multiSchedulingChange.WaitAsync(context.Cancellation);
        bool acquired = false;
        bool released = false;
        bool incremented = false;
        RuntimeMultiSchedulingBlocker? reservation = null;
        try
        {
            await context.Mailbox.WaitForControlCompletion(permit, context.Cancellation);
            await permit;
            acquired = true;
            if (block)
            {
                Task dirtyDrained;
                lock (gate)
                {
                    RequireAlive(context.Self);
                    context.Cancellation.ThrowIfCancellationRequested();
                    if (!multiSchedulingBlockers.TryGetValue(context, out reservation))
                    {
                        reservation = new(this, context);
                        multiSchedulingBlockers.Add(context, reservation);
                        reservation.Registration = context.Cancellation.UnsafeRegister(
                            static state => ((RuntimeMultiSchedulingBlocker)state!).OwnerStopped(), reservation);
                    }
                    context.Cancellation.ThrowIfCancellationRequested();
                    if (normal)
                    {
                        if (reservation.NormalCount == RuntimeMultiSchedulingLimits.MaximumNestedBlocks)
                            throw new ErlangException(ErlangErrorReasons.SystemLimit);
                        reservation.NormalCount++;
                        reservation.NormalPending = true;
                    }
                    else
                    {
                        if (reservation.AllCount == RuntimeMultiSchedulingLimits.MaximumNestedBlocks)
                            throw new ErlangException(ErlangErrorReasons.SystemLimit);
                        reservation.AllCount++;
                        reservation.AllPending = true;
                    }
                    incremented = true;
                    dirtyDrained = RefreshMultiSchedulingDirtyPause();
                }
                await context.Mailbox.WaitForControlCompletion(dirtyDrained, context.Cancellation);
                await dirtyDrained;
                // A real barrier waits for already selected normal actors,
                // controls and synchronous native work. The requester reenters
                // its actual actor lane; no CLR-thread impersonation is used.
                using (await actorDispatcher.AcquireNormalBarrier696(context))
                {
                    lock (gate)
                    {
                        context.Cancellation.ThrowIfCancellationRequested();
                        ClearMultiSchedulingPending(reservation, normal);
                        actorDispatcher.SetMultiSchedulingBlocked(HasCommittedMultiSchedulingBlock);
                        reservation = null;
                    }
                }
            }
            else
            {
                lock (gate)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    if (multiSchedulingBlockers.TryGetValue(context, out var owner))
                    {
                        DecrementMultiScheduling(owner, normal);
                        RemoveEmptyMultiSchedulingBlocker(owner);
                    }
                    _ = RefreshMultiSchedulingDirtyPause();
                    actorDispatcher.SetMultiSchedulingBlocked(HasCommittedMultiSchedulingBlock);
                }
            }
            Term result;
            lock (gate)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                result = MultiSchedulingState();
            }
            // Publication, not eventual requester reentry, owns the change
            // permit. A strongly bound requester may remain parked on lane two
            // indefinitely; other live processes must still change their blocks.
            multiSchedulingChange.Release();
            released = true;
            await context.YieldExecution();
            context.Cancellation.ThrowIfCancellationRequested();

            return result;
        }
        catch
        {
            if (reservation is not null)
            {
                lock (gate)
                {
                    if (multiSchedulingBlockers.TryGetValue(context, out var current) && ReferenceEquals(current, reservation))
                    {
                        if (incremented)
                        {
                            DecrementMultiScheduling(reservation, normal);
                            ClearMultiSchedulingPending(reservation, normal);
                        }
                        RemoveEmptyMultiSchedulingBlocker(reservation);
                        _ = RefreshMultiSchedulingDirtyPause();
                        actorDispatcher.SetMultiSchedulingBlocked(HasCommittedMultiSchedulingBlock);
                    }
                }
            }
            throw;
        }
        finally
        {
            if (!acquired && !released)
            {
                try
                {
                    await permit;
                    acquired = true;
                }
                catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
                {
                }
            }
            if (acquired && !released)
            {
                multiSchedulingChange.Release();
            }
        }
    }

    private static void DecrementMultiScheduling(RuntimeMultiSchedulingBlocker owner, bool normal)
    {
        if (normal && owner.NormalCount != 0)
        {
            owner.NormalCount--;
        }
        else if (!normal && owner.AllCount != 0)
        {
            owner.AllCount--;
        }
    }

    private static void ClearMultiSchedulingPending(RuntimeMultiSchedulingBlocker owner, bool normal)
    {
        if (normal)
            owner.NormalPending = false;
        else
            owner.AllPending = false;
    }

    private bool HasCommittedMultiSchedulingBlock
        => multiSchedulingBlockers.Values.Any(owner => owner.HasAllBlock || owner.HasNormalBlock);

    private void RemoveEmptyMultiSchedulingBlocker(RuntimeMultiSchedulingBlocker owner)
    {
        if (owner.AllCount == 0 && owner.NormalCount == 0)
        {
            multiSchedulingBlockers.Remove(owner.Context);
            // Never synchronously wait for a cancellation callback while holding R.
            owner.Registration.Unregister();
        }
    }

    internal void ReleaseMultiSchedulingBlocker(RuntimeMultiSchedulingBlocker owner)
    {
        lock (gate)
        {
            if (!multiSchedulingBlockers.TryGetValue(owner.Context, out var current) || !ReferenceEquals(current, owner))
            {
                return;
            }
            multiSchedulingBlockers.Remove(owner.Context);
            _ = RefreshMultiSchedulingDirtyPause();
            actorDispatcher.SetMultiSchedulingBlocked(HasCommittedMultiSchedulingBlock);
        }
    }

    private Task RefreshMultiSchedulingDirtyPause()
    {
        bool pause = multiSchedulingBlockers.Values.Any(owner => owner.AllCount != 0);
        if (pause != multiSchedulingDirtyPaused)
        {
            multiSchedulingDirtyPaused = pause;
            if (dirtyNative419 is { } dirty)
            {
                if (pause)
                {
                    return dirty.PauseScheduling();
                }
                dirty.ResumeScheduling();
            }
        }

        return dirtyNative419?.SchedulingPauseCompletion ?? Task.CompletedTask;
    }

    private Term MultiSchedulingState() => Term.A(
        multiSchedulingBlockers.Values.Any(owner => owner.HasAllBlock)
            ? RuntimeMultiSchedulingNames.Blocked
            : HasCommittedMultiSchedulingBlock ? RuntimeMultiSchedulingNames.BlockedNormal : RuntimeMultiSchedulingNames.Enabled);

    internal bool TryMultiSchedulingInfo(Term query, out Term result)
    {
        lock (gate)
        {
            if (query is Atom { Name: RuntimeMultiSchedulingNames.Flag })
            {
                result = MultiSchedulingState();

                return true;
            }
            if (query is Atom { Name: RuntimeMultiSchedulingNames.AllBlockers or RuntimeMultiSchedulingNames.NormalBlockers } atom)
            {
                bool normal = atom.Name == RuntimeMultiSchedulingNames.NormalBlockers;
                result = ActorDispatchCapacity == 1 ? Nil.Value : Cons.From(multiSchedulingBlockers.Values
                    .Where(owner => normal ? owner.HasNormalBlock : owner.HasAllBlock)
                    .Reverse().Select(owner => (Term)owner.Context.Self));

                return true;
            }
        }
        result = Nil.Value;

        return false;
    }
}
