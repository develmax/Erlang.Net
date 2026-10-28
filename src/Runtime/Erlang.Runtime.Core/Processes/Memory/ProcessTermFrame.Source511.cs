namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    private readonly HashSet<int> sourceExpressionSlots511 = new();

    internal Term AdoptExpression511(Term value)
    {
        int slot = Reserve();
        Term result = AdoptGuestInto507(slot, value);
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            sourceExpressionSlots511.Add(slot);

            return result;
        }
    }

    internal int SourceWatermark511
    {
        get
        {
            lock (owner.Gate) return nextSlot;
        }
    }

    internal ProcessTermFrame506 EnterSourceLexicalRoots511() => owner.Enter();

    private ProcessTermConstruction514? constructionCustody514;

    internal ProcessTermConstruction514 BeginConstruction514(Term source)
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (constructionCustody514 is not null)
                throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody);
            owner.ValidateGuestGraph507(source);
            int slot = Reserve();
            PublishRoot512(slot, source);
            try
            {
                return constructionCustody514 = new(this, source);
            }
            catch
            {
                Clear(slot);
                throw;
            }
        }
    }

    internal void PublishConstructedNode511(Term value, ProcessTermConstruction514 custody)
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!ReferenceEquals(custody, constructionCustody514))
                throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody);
            if (!owner.IsOwnedLocal(value))
                return;
            if (value is not (Cons or TupleTerm or MapTerm or HamtBitmapTerm429))
                throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody);
            // Only admitted tuple/cons/map containers are created here. Their
            // direct children must come from the rooted input or earlier nodes;
            // no new binary backing can enter through this capability.
            foreach (Term child in ProcessTermHeap506.DirectConstructionChildren517(value))
                if (!custody.Contains(child))
                    throw new InvalidOperationException(ProcessTermConstruction514Names.UnadmittedChild);
            int slot = Reserve();
            bool added = custody.Admit(value);
            try
            {
                // The transaction's single complete input lease owns all binary
                // descendants. This slot is a real guest-cell root only.
                values.Add(slot, value);
            }
            catch
            {
                if (added)
                    custody.Forget(value);
                throw;
            }
        }
    }

    internal ProcessTermFrame506 PublishLexicalAndRetire511(
        ProcessTermFrame506 lexical,
        int watermark,
        int completedCallResult,
        IReadOnlyList<Term> roots
    )
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ObjectDisposedException.ThrowIf(lexical.disposed, lexical);
            if (!ReferenceEquals(owner, lexical.owner))
                throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
            ValidateSlot(completedCallResult);
            foreach (Term root in roots)
                owner.ValidateGuestGraph507(root);

            // This private lexical frame exports no logical slot handles.
            // Validate every replacement before changing either root set.
            int[] retired = sourceExpressionSlots511.Where(slot => slot >= watermark).ToArray();
            ProcessTermFrame506 replacement = owner.Enter();
            try
            {
                replacement.nextSlot = roots.Count;
                for (int index = 0; index < roots.Count; index++)
                    replacement.PublishRoot512(index, roots[index]);

            }
            catch
            {
                replacement.Dispose();
                throw;
            }
            // Everything below is callback-free retirement under owner.Gate;
            // the complete replacement now owns both Term and binary roots.
            foreach (int slot in retired)
            {
                Clear(slot);
                sourceExpressionSlots511.Remove(slot);
            }
            // Each completed call has already been adopted by Evaluate. Its
            // former handoff slot is redundant after lexical publication.
            Clear(completedCallResult);
            lexical.Dispose();

            return replacement;
        }
    }
}
