namespace Erlang;

// Candidate publication stays local; receive snapshots may hold an interest
// in the exact registered CURRENT slot of another mirror in this same heap.
internal sealed partial class BeamOwnedRoots507 : IDisposable
{
    private readonly ProcessContext context;
    private readonly ProcessTermFrame506 frame;
    private readonly BeamRootFrame553 holder553;
    private readonly int[] first;
    private readonly int[] second;
    private readonly bool[] alternate;
    private readonly bool[] published;
    private readonly bool[] reserved507;
    private readonly BeamRootFrame553.Interest?[] borrowed553;
    private readonly bool[] pending553;
    private readonly bool[] prepared553;
    private readonly bool[] constructed553;
    private readonly int[] pendingSlot553;
    private readonly long[] generation553;
    private readonly Term?[] pendingValue553;
    private readonly Exception?[] cleanupFailures553;
    private long nextGeneration553;
    private int preparingIndex553 = -1;
    private bool disposed553;

    internal BeamOwnedRoots507(ProcessContext context, int count)
    {
        this.context = context;
        frame = context.TermHeap506.Enter();
        try
        {
            holder553 = new(context.TermHeap506, frame);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            first = new int[count];
            second = new int[count];
            alternate = new bool[count];
            published = new bool[count];
            reserved507 = new bool[count];
            borrowed553 = new BeamRootFrame553.Interest?[count];
            pending553 = new bool[count];
            prepared553 = new bool[count];
            constructed553 = new bool[count];
            pendingSlot553 = new int[count];
            generation553 = new long[count];
            pendingValue553 = new Term?[count];
            cleanupFailures553 = new Exception?[checked(count + 1)];
        }
        catch
        {
            frame.Dispose();
            throw;
        }
    }

    private void RequireOpen553()
    {
        ObjectDisposedException.ThrowIf(disposed553, this);
        holder553.RequireOpen();
    }

    private void EnsureSlots507(int index)
    {
        RequireOpen553();
        if (reserved507[index]) return;
        var pair = holder553.ReservePair();
        first[index] = pair.First;
        second[index] = pair.Second;
        reserved507[index] = true;
    }

    // Querying the bank never creates a pending/constructed operation stamp.
    private int Candidate(int index)
    {
        lock (holder553.Gate)
        {
            EnsureSlots507(index);

            return alternate[index] ? first[index] : second[index];
        }
    }

    private int Current(int index) => alternate[index] ? second[index] : first[index];

    private void ForgetPreparation553(int index)
    {
        if (preparingIndex553 == index) preparingIndex553 = -1;
        pending553[index] = false;
        prepared553[index] = false;
        constructed553[index] = false;
        generation553[index] = 0;
        pendingValue553[index] = null;
    }

    private (int Slot, long Generation) CandidateForWrite553(int index)
    {
        lock (holder553.Gate)
        {
            RequireOpen553();
            if (preparingIndex553 >= 0)
                throw new InvalidOperationException(BeamOwnedRoots507Names.PendingPreparation553);
            int selected = Candidate(index);
            if (!holder553.CanWrite(selected))
            {
                selected = holder553.Reserve();
                if (alternate[index]) first[index] = selected;
                else second[index] = selected;
            }
            long generation = checked(nextGeneration553 + 1);
            nextGeneration553 = generation;
            pending553[index] = true;
            preparingIndex553 = index;
            pendingSlot553[index] = selected;
            generation553[index] = generation;

            return (selected, generation);
        }
    }

    private void MarkPrepared553(
        int index,
        int slot,
        long generation,
        Term value,
        bool constructed
    )
    {
        lock (holder553.Gate)
        {
            RequireOpen553();
            if (!pending553[index] || generation <= 0 || generation553[index] != generation
                || pendingSlot553[index] != slot || !ReferenceEquals(frame.Read(slot), value))
                throw new InvalidDataException(BeamConstructedCommit548Names.CandidateMismatch);
            pendingValue553[index] = value;
            prepared553[index] = true;
            constructed553[index] = constructed;
        }
    }

    internal BeamRegisterSlot Prepare(int index, BeamRegisterSlot value, bool adopt)
    {
        var candidate = CandidateForWrite553(index);
        int slot = candidate.Slot;
        try
        {
            if (value.Kind == BeamRegisterSlotKind.Term)
            {
                Term prepared = adopt
                    ? frame.AdoptGuestInto507(slot, value.ReadTerm())
                    : frame.SetLocalGraph507(slot, value.ReadTerm());
                MarkPrepared553(
                    index,
                    slot,
                    candidate.Generation,
                    prepared,
                    false
                );

                return BeamRegisterSlot.FromTerm(prepared);
            }
            frame.SetLocalGraph507(slot, value.RootGraph507());
            MarkPrepared553(
                index,
                slot,
                candidate.Generation,
                frame.Read(slot),
                false
            );

            return value;
        }
        catch (Exception primary)
        {
            try
            {
                Rollback(index);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    internal Term Tuple(int index, IReadOnlyList<Term> children)
    {
        var candidate = CandidateForWrite553(index);
        int slot = candidate.Slot;
        Term value = frame.CreateTupleInto507(slot, children);
        MarkPrepared553(
            index,
            slot,
            candidate.Generation,
            value,
            true
        );

        return value;
    }

    internal Term Cons(int index, Term head, Term tail)
    {
        var candidate = CandidateForWrite553(index);
        int slot = candidate.Slot;
        Term value = frame.CreateConsInto507(slot, head, tail);
        MarkPrepared553(
            index,
            slot,
            candidate.Generation,
            value,
            true
        );

        return value;
    }

    internal void RequireConstructedCandidate548(int index, Term value)
    {
        lock (holder553.Gate)
        {
            RequireOpen553();
            if (!pending553[index]) throw new KeyNotFoundException();
            if (!prepared553[index] || !constructed553[index]
                || generation553[index] <= 0 || pendingSlot553[index] != Candidate(index)
                || !ReferenceEquals(pendingValue553[index], value)
                || !ReferenceEquals(frame.Read(pendingSlot553[index]), value))
                throw new InvalidDataException(BeamConstructedCommit548Names.CandidateMismatch);
        }
    }

    internal bool HoldsPublishedTerm550(int index, Term value)
    {
        lock (holder553.Gate)
        {
            RequireOpen553();

            return published[index] && (borrowed553[index] is { } interest
                ? interest.Matches(value) : ReferenceEquals(frame.Read(Current(index)), value));
        }
    }

    internal bool TryShareCurrent553(int index, Term value, BeamOwnedRoots507 destination)
    {
        if (!ReferenceEquals(context, destination.context)) return false;
        lock (holder553.Gate)
        {
            RequireOpen553();
            destination.RequireOpen553();
            if (destination.published[index] || destination.pending553[index]
                || !HoldsPublishedTerm550(index, value)) return false;
            BeamRootFrame553.Interest next = borrowed553[index] is { } existing
                ? existing.Duplicate() : holder553.BorrowOwned(Current(index), value);
            destination.borrowed553[index] = next;
            destination.published[index] = true;

            return true;
        }
    }

    internal void Commit(int index)
    {
        lock (holder553.Gate)
        {
            RequireOpen553();
            if (!pending553[index] || !prepared553[index] || generation553[index] <= 0
                || pendingSlot553[index] != Candidate(index)
                || !ReferenceEquals(frame.Read(pendingSlot553[index]), pendingValue553[index]))
                throw new InvalidDataException(BeamConstructedCommit548Names.CandidateMismatch);
            bool previousPublished = published[index];
            int previousSlot = Current(index);
            BeamRootFrame553.Interest? previousBorrowed = borrowed553[index];
            borrowed553[index] = null;
            alternate[index] = !alternate[index];
            published[index] = true;
            // A later cleanup error cannot let outer Rollback clear the new
            // CURRENT or treat an old loan as another prepared operation.
            ForgetPreparation553(index);
            if (previousBorrowed is not null) previousBorrowed.Dispose();
            else if (previousPublished) holder553.ReleaseOwned(previousSlot);
        }
    }

    internal void Rollback(int index)
    {
        lock (holder553.Gate)
        {
            RequireOpen553();
            if (!pending553[index])
            {
                frame.RequireLive507();

                return;
            }
            int slot = pendingSlot553[index];
            ForgetPreparation553(index);
            holder553.ReleaseOwned(slot);
        }
    }

    internal void Clear(int index)
    {
        lock (holder553.Gate)
        {
            RequireOpen553();
            ClearCore553(index);
        }
    }

    private void ClearCore553(int index)
    {
        BeamRootFrame553.Interest? borrowed = borrowed553[index];
        borrowed553[index] = null;
        published[index] = false;
        alternate[index] = false;
        ForgetPreparation553(index);
        Exception? firstFailure = null, secondFailure = null, borrowedFailure = null;
        if (reserved507[index])
        {
            firstFailure = ReleaseOwnedSlot553(holder553, first[index]);
            secondFailure = ReleaseOwnedSlot553(holder553, second[index]);
        }
        try
        {
            borrowed?.Dispose();
        }
        catch (Exception error)
        {
            borrowedFailure = error;
        }
        // Allocate any aggregate only after every independent release ran.
        BeamRootFrame553.ThrowCleanup(firstFailure, secondFailure, borrowedFailure);
    }

    private static Exception? ReleaseOwnedSlot553(BeamRootFrame553 holder, int slot)
    {
        try
        {
            holder.ReleaseOwned(slot);

            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }

    private ProcessTermFrame506 ExportFrame553(Term value)
    {
        RequireOpen553();
        for (int index = 0; index < borrowed553.Length; index++)
            if (published[index] && borrowed553[index] is { } current && current.RefersTo(value))
                return current.Frame(value);

        // Mixed/derived exports retain the existing full borrow/publication
        // path in the destination's own registered anchor frame.
        return frame;
    }

    internal Term CompleteGuestReturn507(Term value)
    {
        RequireOpen553();
        var invocation = context.CurrentOwned506;
        if (invocation is { IsDisposed: false, CanBorrow507: true }
            && invocation.BindingKind507 != OwnedBindingKind507.Opaque
            && invocation.OwnsContext507(context)
            && RuntimeActorExecutionTurn.IsOwnedBy(context))
            return invocation.HoldGuestResult507(value);

        return Export(value);
    }

    internal Term Export(Term value) => context.TermHeap506.Export(ExportFrame553(value), value);

    internal Term[] ExportArguments(IReadOnlyList<Term> values)
        => ((TupleTerm)Export(Term.Tuple(values.ToArray()))).Items.ToArray();

    internal IDisposable Protect(Term value)
    {
        RequireOpen553();

        return new BeamOwnedTemporary507(context, value);
    }

    internal ValueTask ReceiveCheckpoint507(string phase) => BeamOwnedCheckpoint507.Reach(context, phase);

    public void Dispose()
    {
        lock (holder553.Gate)
        {
            if (disposed553) return;
            disposed553 = true;
            int failureCount = 0;
            try
            {
                for (int index = 0; index < published.Length; index++)
                    try
                    {
                        ClearCore553(index);
                    }
                    catch (Exception error)
                    {
                        cleanupFailures553[failureCount++] = error;
                    }
            }
            finally
            {
                try
                {
                    holder553.Close();
                }
                catch (Exception error)
                {
                    cleanupFailures553[failureCount++] = error;
                }
            }
            try
            {
                if (failureCount == 1)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupFailures553[0]!).Throw();
                if (failureCount > 1)
                    throw new AggregateException(cleanupFailures553.Take(failureCount).Select(error => error!));
            }
            finally
            {
                Array.Clear(cleanupFailures553);
            }
        }
    }
}
