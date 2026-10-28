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
        ProcessTermFrame506 construction;
        ProcessTermConstruction514 custody;
        lock (owner.Gate)
        {
            ValidateSlot(slot);
            owner.ValidateGuestGraph507(value);
            if (owner.IsOwnedLocal(value) || owner.IsLiteralFloat556(value)
                || owner.IsLiteralInteger559(value) || ProcessTermHeap506.IsImmediateInteger559(value)
                || value is not (TupleTerm or Cons or MapTerm or FloatTerm or Integer)
                || value is TupleTerm { Items.Count: 0 } || value is MapTerm { Entries.Count: 0 })
            {
                PublishRoot512(slot, value);

                return value;
            }
            construction = owner.Enter();
            try
            {
                custody = construction.BeginConstruction514(value);
            }
            catch
            {
                construction.Dispose();
                throw;
            }
        }
        using (construction)
        using (PreparedMapGraph517 maps = owner.PrepareMapGraph517(value, true))
        using (PreparedMapGraphUse517 admitted = maps.AdmitBuilds517())
        {
            lock (owner.Gate)
            {
                ValidateSlot(slot);
                Term adopted = owner.AdoptGuest507(
                    value,
                    custody,
                    maps,
                    admitted
                );
                PublishRoot512(slot, adopted);

                return adopted;
            }
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
