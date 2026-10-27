namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    // Logical root-slot identifiers, not physical heap/stack capacity.
    internal int IssuedRootSlotIds507
    {
        get
        {
            lock (owner.Gate) return nextSlot;
        }
    }

    internal Term SetLocalGraph507(int slot, Term value)
    {
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            owner.ValidateGuestGraph507(value);
            PublishRoot512(slot, value);

            return value;
        }
    }

    internal Term RetainLocalGraph507(Term value) => SetLocalGraph507(Reserve(), value);

    internal Term CreateTupleInto507(int slot, IReadOnlyList<Term> children)
    {
        if (children is not Term[] input)
            throw new ArgumentException(OwnedBoundary507Names.ArraySnapshotRequired, nameof(children));
        Term[] copied = (Term[])input.Clone();
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            foreach (Term child in copied)
                owner.ValidateGuestGraph507(child);
            Term value = owner.CreateTuple(copied);
            PublishRoot512(slot, value);

            return value;
        }
    }

    internal Term CreateConsInto507(int slot, Term head, Term tail)
    {
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            owner.ValidateGuestGraph507(head);
            owner.ValidateGuestGraph507(tail);
            Term value = owner.CreateCons(head, tail);
            PublishRoot512(slot, value);

            return value;
        }
    }

    internal Term AdoptGuestInto507(int slot, Term value)
    {
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            owner.ValidateGuestGraph507(value);
            if (owner.IsOwnedLocal(value) || value is not (TupleTerm or Cons) || value is TupleTerm { Items.Count: 0 })
            {
                PublishRoot512(slot, value);

                return value;
            }
            using ProcessTermFrame506 construction = owner.Enter();
            // Root the complete validated input before the first allocation.
            // Intermediate newly constructed nodes join the same transaction.
            construction.PublishRoot512(construction.Reserve(), value);
            Term adopted = owner.AdoptGuest507(value, construction);
            PublishRoot512(slot, adopted);

            return adopted;
        }
    }

    internal void ClearAllRoots507()
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            values.Clear();
            binaryRoots512?.ClearAll512();
        }
    }

    // Only the permanent dictionary frame uses this reset. It exports no slot
    // handles; pending reads use independent registered borrow frames.
    internal void ResetDictionaryRoots507()
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            values.Clear();
            binaryRoots512?.ClearAll512();
            nextSlot = 0;
        }
    }
}
