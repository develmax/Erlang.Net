namespace Erlang;

// Pilot payload backend: real managed pointer-word slabs, explicit registered
// root frames. No implicit CLR root scanning and no public resource estimates.
internal sealed class ProcessTermHeap506(ProcessRuntime runtime)
{
    internal object Gate { get; } = new();
    private readonly List<ProcessTermSlab506> slabs = new();
    private readonly Dictionary<long, ProcessTermSequence506> blocks = new();
    private readonly HashSet<ProcessTermFrame506> frames = new();
    private long identity;
    private bool closing;
    private readonly TaskCompletionSource drained = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal ProcessTermHeapSnapshot506 Snapshot
    {
        get
        {
            lock (Gate)
                return new(
                    slabs.Sum(slab => (long)slab.Cells.Length),
                    blocks.Values.Sum(block => (long)block.Count),
                    blocks.Count,
                    frames.Count
                );
        }
    }

    internal ProcessTermFrame506 Enter()
    {
        lock (Gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            var frame = new ProcessTermFrame506(this);
            frames.Add(frame);

            return frame;
        }
    }

    internal ProcessTermBorrow506 Borrow(ProcessTermFrame506 caller, Term value)
    {
        lock (Gate)
        {
            if (!frames.Contains(caller) || caller.IsDisposed)
                throw new ObjectDisposedException(nameof(ProcessTermFrame506));
            ValidateLocalGraph(value);
            var frame = new ProcessTermFrame506(this);
            frame.InitializeBorrow506(value);
            var borrow = new ProcessTermBorrow506(this, frame, value);
            frames.Add(frame);

            return borrow;
        }
    }

    internal PreparedTermTransfer506 Prepare(ProcessTermFrame506 caller, Term value)
        => PrepareBorrow(Borrow(caller, value));

    internal PreparedTermTransfer506 PrepareBorrow(ProcessTermBorrow506 borrow)
    {
        if (!ReferenceEquals(borrow.Owner, this))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        ObjectDisposedException.ThrowIf(borrow.IsDisposed, borrow);
        try
        {
            // No heap or dictionary gate is held by callers here. Typed recipe
            // factories and external-node discovery can acquire runtime locks.
            Term portable = OwnedTermDetachedCopy506.Copy(runtime, borrow.Value);

            return new(borrow, portable);
        }
        catch
        {
            borrow.Dispose();
            throw;
        }
    }

    internal Term ImportPrepared(Term portable)
    {
        // Caller holds Gate. This pass only allocates tuple/list payloads;
        // opaque portable subgraphs retain their portable descriptor/backing.
        return OwnedTermTransfer506.Copy(portable, this);
    }

    internal bool IsOwnedLocal(Term value)
    {
        if (Storage(value) is not { } block)
            return false;
        if (!ReferenceEquals(block.Owner, this))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        if (!blocks.TryGetValue(block.Identity, out var active) || !ReferenceEquals(active, block))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));

        return true;
    }

    internal Term Export(ProcessTermFrame506 caller, Term value)
    {
        using PreparedTermTransfer506 prepared = Prepare(caller, value);

        return prepared.Portable;
    }

    private void ValidateLocalGraph(Term value)
    {
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(value);
        while (pending.TryPop(out Term? current))
        {
            if (!seen.Add(current))
                continue;
            // Check ownership before reading a sequence; never take another
            // process heap gate while holding this process's gate.
            if (Storage(current) is not null)
                _ = IsOwnedLocal(current);
            foreach (Term child in new DebugDirtyCopyFrame424(current).Children)
                pending.Push(child);
        }
    }

    internal Term CreateTuple(Term[] children) => children.Length == 0 ? Term.Tuple() : new TupleTerm(Allocate(children));

    internal Term CreateCons(Term head, Term tail) => new Cons(Allocate([head, tail]));

    private ProcessTermSequence506 Allocate(Term[] children)
    {
        // Caller holds Gate throughout complete graph import + root publication.
        ProcessTermSlab506? selected = null;
        int offset = -1;
        foreach (ProcessTermSlab506 slab in slabs)
        {
            offset = slab.Reserve(children.Length);
            if (offset < 0)
                continue;
            selected = slab;
            break;
        }
        if (selected is null)
        {
            int size = ProcessTerm506Names.InitialReferenceSlabWords;
            int previous = ProcessTerm506Names.PreviousFibonacciSlabWords;
            while (size < children.Length)
            {
                int next = size < ProcessTerm506Names.LastFibonacciSlabWords
                    ? checked(size + previous + 1)
                    : checked(size + size / ProcessTerm506Names.PostFibonacciGrowthDivisor);
                previous = size;
                size = next;
            }
            selected = new(size);
            slabs.Add(selected);
            offset = selected.Reserve(children.Length);
        }
        try
        {
            long id = checked(identity + 1);
            var block = new ProcessTermSequence506(
                this,
                id,
                selected,
                offset,
                children.Length
            );
            Array.Copy(
                children,
                0,
                selected.Cells,
                offset,
                children.Length
            );
            blocks.Add(id, block);
            identity = id;

            return block;
        }
        catch
        {
            selected.Release(offset, children.Length);
            throw;
        }
    }

    internal Term Read(ProcessTermSequence506 block, int index)
    {
        lock (Gate)
        {
            if (!blocks.TryGetValue(block.Identity, out var current) || !ReferenceEquals(current, block))
                throw new ObjectDisposedException(nameof(ProcessTermSequence506));
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, block.Count);

            return block.Slab.Cells[block.Offset + index]!;
        }
    }

    internal long Collect()
    {
        lock (Gate)
        {
            var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
            var marked = new HashSet<long>();
            var pending = new Stack<Term>(frames.SelectMany(frame => frame.Roots));
            while (pending.TryPop(out Term? term))
            {
                if (!seen.Add(term))
                    continue;
                if (Storage(term) is { } block)
                {
                    if (!ReferenceEquals(block.Owner, this))
                        throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
                    marked.Add(block.Identity);
                }
                foreach (Term child in new DebugDirtyCopyFrame424(term).Children)
                    pending.Push(child);
            }
            long[] dead = blocks.Keys.Where(id => !marked.Contains(id)).ToArray();
            long reclaimed = 0;
            foreach (long id in dead)
            {
                ProcessTermSequence506 block = blocks[id];
                reclaimed = checked(reclaimed + block.Count);
                block.Slab.Release(block.Offset, block.Count);
                blocks.Remove(id);
            }

            return reclaimed;
        }
    }

    internal void Release(ProcessTermFrame506 frame)
    {
        frames.Remove(frame);
        FinishRetirement();
    }

    internal Task RetireAsync()
    {
        lock (Gate)
        {
            closing = true;
            FinishRetirement();

            return drained.Task;
        }
    }

    private void FinishRetirement()
    {
        if (!closing || frames.Count != 0)
            return;
        foreach (ProcessTermSlab506 slab in slabs)
            Array.Clear(slab.Cells);
        blocks.Clear();
        slabs.Clear();
        drained.TrySetResult();
    }

    internal static IOwnedTermSequence506? Storage(Term term) => term switch
    {
        TupleTerm tuple => tuple.OwnedStorage506,
        Cons cons => cons.OwnedStorage506,
        _ => null
    };
}
