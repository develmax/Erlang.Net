namespace Erlang;

internal sealed partial class MailboxMessageFragment
{
    // Preparation only: called before R/M while this fragment is unpublished.
    internal Term ImportSequenceTraceLeaf(Term portable)
    {
        ProcessTermFrame506 current = frame ?? throw new ObjectDisposedException(nameof(MailboxMessageFragment));
        int slot = current.Reserve();
        current.SetPrepared(slot, portable);

        return current.Read(slot);
    }

    internal ProcessTermConstruction514 BeginSequenceTraceConstruction()
        => (frame ?? throw new ObjectDisposedException(nameof(MailboxMessageFragment))).BeginConstruction514(Nil.Value);

    internal Term BindSequenceTraceNotification(
        Term notification,
        IReadOnlyDictionary<Term, Term> preparedLeaves,
        ProcessTermConstruction514 custody
    )
    {
        ProcessTermFrame506 current = frame ?? throw new ObjectDisposedException(nameof(MailboxMessageFragment));
        var built = new Dictionary<Term, Term>(ReferenceEqualityComparer.Instance);
        int budget = SequenceTraceRootNames.MaximumOutputWrapperNodes;
        lock (heap.Gate)
        {
            Term result = Build(notification, 0);
            Value = result;

            return result;
        }

        Term Build(Term source, int depth)
        {
            if (preparedLeaves.TryGetValue(source, out Term? prepared)) return prepared;
            if (built.TryGetValue(source, out Term? reused)) return reused;
            if (depth > SequenceTraceRootNames.MaximumOutputWrapperDepth || --budget < 0)
                throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
            // Never inspect a foreign/owned header that was not prepared.
            if (ProcessTermHeap506.Storage(source) is not null)
                throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
            Term value;
            if (source is Atom or Nil || source is Pid pid && ReferenceEquals(pid.LocalIdentity, heap.Runtime524.LocalIdentity))
            {
                value = source;
                custody.Admit(value);
            }
            else if (source is Integer number && number.Value >= long.MinValue && number.Value <= ulong.MaxValue)
            {
                value = heap.CreateInteger559(number.Value, custody);
                custody.Admit(value);
            }
            else if (heap.IsLocalPidReference775(source))
            {
                value = heap.CreatePidReference775((ReferenceTerm)source, custody);
            }
            else if (heap.IsOrdinaryLocalReference562(source))
            {
                value = heap.CreateReference562((ReferenceTerm)source, custody);
            }
            else if (source is TupleTerm tuple)
            {
                if (tuple.Items.Count > SequenceTraceRootNames.MaximumOutputWrapperNodes)
                    throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
                var children = new Term[tuple.Items.Count];
                for (int index = 0; index < children.Length; index++) children[index] = Build(tuple.Items[index], depth + 1);
                value = current.CreateSequenceTraceTuple(custody, children);
            }
            else if (source is Cons cons)
            {
                Term head = Build(cons.Head, depth + 1), tail = Build(cons.Tail, depth + 1);
                value = current.CreateSequenceTraceCons(custody, head, tail);
            }
            else throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
            custody.Admit(value);
            built.Add(source, value);

            return value;
        }
    }
}
