namespace Erlang;

public sealed partial class ProcessDictionary
{
    internal Term ValueForProcessInfo611(ProcessInfoSnapshot601 origin, Term key)
    {
        Term portableKey = Detach(key);
        ProcessTermBorrow506 borrow;
        lock (gate)
        {
            CheckOpen();
            if (!data.TryGetValue(portableKey, out Term? found))
                return ProcessInfoAtoms.Undefined;
            if (frame is null)
                return found;
            borrow = context.TermHeap506.Borrow(frame, found);
        }
        using (borrow)
        {
            AtomicsHostCopyTransaction591 copies = origin.CreateDictionaryHostCopies611();

            return OwnedTermDetachedCopy506.CopyAtomics586(
                context.Runtime,
                borrow.Value,
                exportHostAtomics586: true,
                hostCopies591: copies
            );
        }
    }

    internal Term SnapshotForProcessInfo611(ProcessInfoSnapshot601 origin)
    {
        ProcessTermBorrow506 borrow;
        lock (gate)
        {
            CheckOpen();
            // With no owned dictionary frame these are existing caller-owned
            // values. This route has not minted any new Host carrier to own.
            if (frame is null)
                return Cons.From(data.Select(pair => Term.Tuple(pair.Key, pair.Value)));
            borrow = context.TermHeap506.Borrow(frame,
                Term.Tuple(data.SelectMany(pair => new[] { pair.Key, pair.Value }).ToArray()));
        }
        using (borrow)
        {
            // Register the empty transaction BEFORE the first new Host clone.
            // On any later failure origin cleanup still owns those exact
            // carriers and releases them outside runtime/mailbox monitors.
            AtomicsHostCopyTransaction591 copies = origin.CreateDictionaryHostCopies611();
            var portable = (TupleTerm)OwnedTermDetachedCopy506.CopyAtomics586(
                context.Runtime,
                borrow.Value,
                exportHostAtomics586: true,
                hostCopies591: copies
            );

            return Cons.From(Enumerable.Range(0, portable.Items.Count / 2)
                .Select(index => Term.Tuple(portable.Items[index * 2], portable.Items[index * 2 + 1])));
        }
    }
}
