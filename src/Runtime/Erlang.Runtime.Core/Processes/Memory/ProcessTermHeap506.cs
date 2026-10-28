namespace Erlang;

// Pilot payload backend: real managed pointer-word slabs, explicit registered
// root frames. No implicit CLR root scanning and no public resource estimates.
internal sealed partial class ProcessTermHeap506(ProcessRuntime runtime)
{
    internal object Gate { get; } = new();
    private readonly List<ProcessTermSlab506> slabs = new();
    private readonly Dictionary<long, ProcessTermSequence506> blocks = new();
    private readonly HashSet<ProcessTermFrame506> frames = new();
    internal long AllocationSlabVisits519 { get; private set; }
    private readonly Dictionary<int, int> allocationCandidates519 = new();
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
            try
            {
                frames.Add(frame);

                return frame;
            }
            catch
            {
                frame.Dispose();
                throw;
            }
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
            try
            {
                frame.InitializeBorrow506(value);
                var borrow = new ProcessTermBorrow506(this, frame, value);
                frames.Add(frame);

                return borrow;
            }
            catch
            {
                frame.Dispose();
                throw;
            }
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
            foreach (Term child in NativeMapOwnedRoots510.Children(current))
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
        int selectedIndex = allocationCandidates519.TryGetValue(children.Length, out int candidate)
            ? candidate : 0;
        for (; selectedIndex < slabs.Count; selectedIndex++)
        {
            ProcessTermSlab506 slab = slabs[selectedIndex];
            unchecked
            {
                AllocationSlabVisits519++;
            }
            offset = slab.Reserve(children.Length);
            if (offset < 0)
            {
                allocationCandidates519[children.Length] = selectedIndex + 1;
                continue;
            }
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
            selectedIndex = slabs.Count;
            selected = new(size);
            slabs.Add(selected);
            unchecked
            {
                AllocationSlabVisits519++;
            }
            offset = selected.Reserve(children.Length);
        }
        try
        {
            // This insertion can allocate. It is inside the reservation rollback.
            allocationCandidates519[children.Length] = selectedIndex;
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
            ReleaseAllocation519(selected, offset, children.Length);
            throw;
        }
    }

    private void ReleaseAllocation519(ProcessTermSlab506 slab, int offset, int length)
    {
        slab.Release(offset, length);
        allocationCandidates519.Clear();
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
     => CollectGeneration508(true, ProcessFullSweepNames.DefaultThreshold).ReclaimedPayloadWords;


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
        oldBlocks508.Clear();
        slabs.Clear();
        allocationCandidates519.Clear();
        drained.TrySetResult();
    }

    internal static IOwnedTermSequence506? Storage(Term term) => term switch
    {
        TupleTerm tuple => tuple.OwnedStorage506,
        Cons cons => cons.OwnedStorage506,
        _ => null
    };
}
