namespace Erlang;

// Owns actual registered slots, not copied binary/literal metadata. Loaned
// slots outlive the originating mirror only until their last real interest.
internal sealed class BeamRootFrame553(ProcessTermHeap506 heap, ProcessTermFrame506 frame)
{
    private readonly HashSet<int> issued = new();
    private readonly HashSet<int> tombstones = new();
    private readonly Dictionary<int, Slot> slots = new();
    private Exception?[] cleanupFailures = new Exception?[1];
    private int interests;
    private bool closed;
    private bool frameDisposed;

    internal object Gate => heap.Gate;
    private ProcessTermFrame506 RegisteredFrame => frame;

    internal void RequireOpen()
    {
        lock (Gate)
            ObjectDisposedException.ThrowIf(closed || frameDisposed, this);
    }

    internal (int First, int Second) ReservePair()
    {
        lock (Gate)
        {
            RequireOpen();
            issued.EnsureCapacity(checked(issued.Count + 2));
            tombstones.EnsureCapacity(checked(issued.Count + 2));
            if (cleanupFailures.Length < issued.Count + 3) cleanupFailures = new Exception?[issued.Count + 3];
            var pair = frame.ReservePair507();
            issued.Add(pair.First);
            issued.Add(pair.Second);

            return pair;
        }
    }

    internal int Reserve()
    {
        lock (Gate)
        {
            RequireOpen();
            issued.EnsureCapacity(checked(issued.Count + 1));
            tombstones.EnsureCapacity(checked(issued.Count + 1));
            if (cleanupFailures.Length < issued.Count + 2) cleanupFailures = new Exception?[issued.Count + 2];
            int slot = frame.Reserve();
            issued.Add(slot);

            return slot;
        }
    }

    internal bool CanWrite(int slot)
    {
        lock (Gate)
        {
            RequireOpen();

            return issued.Contains(slot) && !tombstones.Contains(slot)
                && (!slots.TryGetValue(slot, out Slot? current) || current.Interests == 0);
        }
    }

    internal Interest BorrowOwned(int slot, Term value)
    {
        lock (Gate)
        {
            RequireOpen();
            if (!issued.Contains(slot) || tombstones.Contains(slot)
                || !ReferenceEquals(frame.Read(slot), value))
                throw new InvalidOperationException(BeamOwnedRoots507Names.InvalidCurrent553);
            bool fresh = !slots.TryGetValue(slot, out Slot? current);
            if (fresh)
                current = new(this, slot, value);
            else if (current!.OwnerReleased || current.Retired || !ReferenceEquals(current.Value, value))
                throw new InvalidOperationException(BeamOwnedRoots507Names.InvalidCurrent553);
            int nextSlot = checked(current!.Interests + 1), nextTotal = checked(interests + 1);
            var result = new Interest(current);
            if (fresh)
            {
                slots.EnsureCapacity(checked(slots.Count + 1));
                slots.Add(slot, current);
            }
            current.Interests = nextSlot;
            interests = nextTotal;

            return result;
        }
    }

    private Interest Duplicate(Slot slot)
    {
        lock (Gate)
        {
            RequireSlot(slot);
            int nextSlot = checked(slot.Interests + 1), nextTotal = checked(interests + 1);
            var result = new Interest(slot);
            slot.Interests = nextSlot;
            interests = nextTotal;

            return result;
        }
    }

    private void RequireSlot(Slot slot)
    {
        // Originating owner may be closed; its real frame remains registered
        // while positive interests exist. This admits clone-of-clone safely.
        if (frameDisposed || slot.Retired || slot.Interests <= 0
            || !slots.TryGetValue(slot.Id, out Slot? current) || !ReferenceEquals(current, slot)
            || !ReferenceEquals(frame.Read(slot.Id), slot.Value))
            throw new ObjectDisposedException(nameof(Interest));
    }

    internal void ReleaseOwned(int slot)
    {
        lock (Gate)
        {
            RequireOpen();
            if (slots.TryGetValue(slot, out Slot? current))
            {
                current.OwnerReleased = true;
                if (current.Interests != 0) return;
            }
            ClearSlot(slot);
        }
    }

    private void ClearSlot(int slot)
    {
        try
        {
            frame.Clear(slot);
        }
        catch
        {
            // Capacity was reserved before issuing the physical ID. Never
            // recycle a slot after failed cleanup, even if its value is gone.
            tombstones.Add(slot);
            throw;
        }
    }

    private void Release(Slot slot)
    {
        lock (Gate)
        {
            if (frameDisposed || slot.Retired || slot.Interests <= 0
                || !slots.TryGetValue(slot.Id, out Slot? current) || !ReferenceEquals(current, slot))
                throw new ObjectDisposedException(nameof(Interest));
            Exception? validationFailure = null;
            try
            {
                if (!ReferenceEquals(frame.Read(slot.Id), slot.Value))
                    throw new InvalidOperationException(BeamOwnedRoots507Names.InvalidCurrent553);
            }
            catch (Exception error)
            {
                validationFailure = error;
            }
            slot.Interests--;
            interests--;
            if (slot.Interests != 0)
            {
                ThrowCleanup(validationFailure);

                return;
            }
            slots.Remove(slot.Id);
            slot.Retired = true;
            slot.Value = null;
            Exception? clearFailure = null, disposeFailure = null;
            try
            {
                if (slot.OwnerReleased || closed) ClearSlot(slot.Id);
            }
            catch (Exception error)
            {
                clearFailure = error;
            }
            try
            {
                FinishClosedFrame();
            }
            catch (Exception error)
            {
                disposeFailure = error;
            }
            ThrowCleanup(validationFailure, clearFailure, disposeFailure);
        }
    }

    internal void Close()
    {
        lock (Gate)
        {
            if (closed) return;
            closed = true;
            frame.ClosePublications553();
            int failureCount = 0;
            try
            {
                foreach (int id in issued)
                {
                    if (slots.TryGetValue(id, out Slot? current) && current.Interests != 0)
                    {
                        current.OwnerReleased = true;
                        continue;
                    }
                    try
                    {
                        ClearSlot(id);
                    }
                    catch (Exception error)
                    {
                        cleanupFailures[failureCount++] = error;
                    }
                }
            }
            finally
            {
                try
                {
                    FinishClosedFrame();
                }
                catch (Exception error)
                {
                    cleanupFailures[failureCount++] = error;
                }
            }
            try
            {
                if (failureCount == 1)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupFailures[0]!).Throw();
                if (failureCount > 1)
                    throw new AggregateException(cleanupFailures.Take(failureCount).Select(error => error!));
            }
            finally
            {
                Array.Clear(cleanupFailures);
            }
        }
    }

    private void FinishClosedFrame()
    {
        if (!closed || interests != 0 || frameDisposed) return;
        frameDisposed = true;
        frame.Dispose();
    }

    internal static void ThrowCleanup(Exception? first, Exception? second = null, Exception? third = null)
    {
        if (first is null)
        {
            if (second is null)
            {
                first = third;
                third = null;
            }
            else
            {
                first = second;
                second = third;
                third = null;
            }
        }
        if (first is null) return;
        if (second is null)
        {
            second = third;
            third = null;
        }
        if (second is null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
        if (third is null) throw new AggregateException(first, second!);
        throw new AggregateException(first, second!, third);
    }

    internal sealed class Slot(BeamRootFrame553 owner, int id, Term value)
    {
        internal readonly BeamRootFrame553 Owner = owner;
        internal readonly int Id = id;
        internal Term? Value = value;
        internal int Interests;
        internal bool OwnerReleased;
        internal bool Retired;
    }

    internal sealed class Interest : IDisposable
    {
        private Slot? slot;

        internal Interest(Slot slot) => this.slot = slot;

        internal Interest Duplicate()
        {
            Slot current = Volatile.Read(ref slot) ?? throw new ObjectDisposedException(nameof(Interest));

            return current.Owner.Duplicate(current);
        }

        internal bool Matches(Term value)
        {
            Slot current = Volatile.Read(ref slot) ?? throw new ObjectDisposedException(nameof(Interest));
            lock (current.Owner.Gate)
            {
                current.Owner.RequireSlot(current);

                return ReferenceEquals(current.Value, value);
            }
        }

        // A search hint only. Frame(value) performs the actual slot admission
        // before an export can use this holder as its registered caller.
        internal bool RefersTo(Term value)
        {
            Slot? current = Volatile.Read(ref slot);
            if (current is null) return false;
            lock (current.Owner.Gate)
                return ReferenceEquals(current.Value, value);
        }

        internal ProcessTermFrame506 Frame(Term value)
        {
            Slot current = Volatile.Read(ref slot) ?? throw new ObjectDisposedException(nameof(Interest));
            lock (current.Owner.Gate)
            {
                current.Owner.RequireSlot(current);
                if (!ReferenceEquals(current.Value, value))
                    throw new InvalidOperationException(BeamOwnedRoots507Names.InvalidCurrent553);

                return current.Owner.RegisteredFrame;
            }
        }

        public void Dispose()
        {
            Slot? previous = Interlocked.Exchange(ref slot, null);
            if (previous is not null) previous.Owner.Release(previous);
        }
    }
}
