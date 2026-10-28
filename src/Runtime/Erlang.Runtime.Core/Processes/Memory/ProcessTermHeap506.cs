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

    internal Term ImportPrepared(
        Term portable,
        PreparedMapGraph517 maps,
        ProcessTermConstruction514 construction,
        PreparedMapGraphUse517 admitted
    )
    {
        // Hash plans already exist; no runtime/index callback under this gate.
        // Legacy unsupported map layouts keep their exact existing path.
        return OwnedTermTransfer506.Copy(
            portable,
            this,
            maps,
            construction,
            admitted
        );
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

    internal Term CreateTuple(Term[] children) => children.Length == 0 ? Term.Tuple() : new TupleTerm(AllocateEncoded526(children, [EncodedMapHeader526.Tuple(children.Length)]));

    internal Term CreateCons(Term head, Term tail) => new Cons(Allocate([head, tail]));

    private ProcessTermSequence506 Allocate(Term[] children) => AllocateEncoded526(children, []);

    private ProcessTermSequence506 AllocateEncoded526(Term[] children, nuint[] headers, nuint[]? numeric = null)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        ObjectDisposedException.ThrowIf(closing && frames.Count == 0, this);
        numeric ??= [];
        int physicalWords = checked(children.Length + headers.Length + numeric.Length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(physicalWords);
        // Caller holds Gate throughout complete graph import + root publication.
        ProcessTermSlab506? selected = null;
        int offset = -1;
        int selectedIndex = allocationCandidates519.TryGetValue(physicalWords, out int candidate)
            ? candidate : 0;
        for (; selectedIndex < slabs.Count; selectedIndex++)
        {
            ProcessTermSlab506 slab = slabs[selectedIndex];
            unchecked
            {
                AllocationSlabVisits519++;
            }
            offset = slab.Reserve(physicalWords);
            if (offset < 0)
            {
                allocationCandidates519[physicalWords] = selectedIndex + 1;
                continue;
            }
            selected = slab;
            break;
        }
        if (selected is null)
        {
            int size = ProcessTerm506Names.InitialReferenceSlabWords;
            int previous = ProcessTerm506Names.PreviousFibonacciSlabWords;
            while (size < physicalWords)
            {
                int next = size < ProcessTerm506Names.LastFibonacciSlabWords
                    ? checked(size + previous + 1)
                    : checked(size + size / ProcessTerm506Names.PostFibonacciGrowthDivisor);
                previous = size;
                size = next;
            }
            selectedIndex = slabs.Count;
            selected = new(size);
            try
            {
                slabs.Add(selected);
            }
            catch
            {
                selected.Cells.Dispose();
                throw;
            }
            unchecked
            {
                AllocationSlabVisits519++;
            }
            offset = selected.Reserve(physicalWords);
        }
        try
        {
            // This insertion can allocate. It is inside the reservation rollback.
            allocationCandidates519[physicalWords] = selectedIndex;
            long id = checked(identity + 1);
            var block = new ProcessTermSequence506(
                this,
                id,
                selected,
                checked(offset + headers.Length + numeric.Length),
                children.Length,
                headers.Length,
                numeric.Length
            );
            selected.Cells.WriteHeaders526(headers, offset);
            selected.Cells.WriteNumeric556(numeric, checked(offset + headers.Length));
            selected.Cells.WriteTerms526(children, checked(offset + headers.Length + numeric.Length));
            blocks.Add(id, block);
            identity = id;

            return block;
        }
        catch
        {
            ReleaseAllocation519(selected, offset, physicalWords);
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
            slab.Cells.Dispose();
        blocks.Clear();
        mapHeaders517.Clear();
        oldBlocks508.Clear();
        slabs.Clear();
        allocationCandidates519.Clear();
        drained.TrySetResult();
    }

    internal static IOwnedTermSequence506? Storage(Term term) => term switch
    {
        TupleTerm tuple => tuple.OwnedStorage506,
        MapTerm map => map.OwnedMapPayload517?.RootPayload,
        HamtBitmapTerm429 bitmap => bitmap.OwnedStorage517,
        Cons cons => cons.OwnedStorage506,
        FloatTerm number => number.OwnedStorage556,
        Integer number => number.OwnedStorage559,
        _ => null
    };
}
