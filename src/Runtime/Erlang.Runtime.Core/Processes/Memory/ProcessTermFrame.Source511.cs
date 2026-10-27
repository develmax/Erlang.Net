namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    private readonly HashSet<int> sourceExpressionSlots511 = new();

    internal Term AdoptExpression511(Term value)
    {
        lock (owner.Gate)
        {
            int slot = Reserve();
            Term result = AdoptGuestInto507(slot, value);
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

    internal void PublishConstructedNode511(Term value)
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            // Only the internal guest adopter calls this immediately after
            // constructing a node from its already validated/rooted children.
            // Empty tuples have no owned payload block to publish.
            if (owner.IsOwnedLocal(value))
                PublishRoot512(Reserve(), value);
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
