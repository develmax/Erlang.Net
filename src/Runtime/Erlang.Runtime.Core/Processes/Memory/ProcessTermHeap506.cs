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
        => BorrowWithLiteral801(caller, value, null);

    internal ProcessTermBorrow506 BorrowWithLiteral801(ProcessTermFrame506 caller, Term value, ModuleLiteralRoots801? carriedLiteral801)
    {
        lock (Gate)
            if (!frames.Contains(caller) || caller.IsDisposed)
                throw new ObjectDisposedException(nameof(ProcessTermFrame506));
        using ModuleLiteralRoots801? literalRoots = ModuleLiteralRoots801.Capture801(this, value, carriedLiteral801);
        lock (Gate)
        {
            if (!frames.Contains(caller) || caller.IsDisposed)
                throw new ObjectDisposedException(nameof(ProcessTermFrame506));
            using var literalAdmission = EnterLiteralAdmission801(literalRoots);
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
        => PrepareBorrowAtomics586(borrow, false);

    internal PreparedTermTransfer506 PrepareBorrowAtomics586(ProcessTermBorrow506 borrow, bool? exportHostAtomics586)
    {
        if (!ReferenceEquals(borrow.Owner, this))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        ObjectDisposedException.ThrowIf(borrow.IsDisposed, borrow);
        try
        {
            // No heap or dictionary gate is held by callers here. Typed recipe
            // factories and external-node discovery can acquire runtime locks.
            Term portable = OwnedTermDetachedCopy506.CopyAtomics586(runtime, borrow.Value, exportHostAtomics586);

            return new(borrow, portable);
        }
        catch
        {
            borrow.Dispose();
            throw;
        }
    }

    // Internal guest send only. The actual borrow owns the admitted source
    // literals until the synchronous publication capture takes its own holder.
    internal PreparedTermTransfer506 PrepareGuestSendBorrow824(ProcessTermBorrow506 borrow)
    {
        if (!ReferenceEquals(borrow.Owner, this))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        ObjectDisposedException.ThrowIf(borrow.IsDisposed, borrow);
        ModuleLiteralRoots801? literalRoots = null;
        try
        {
            literalRoots = ModuleLiteralRoots801.Capture801(this, borrow.Value);
            Term portable = OwnedTermDetachedCopy506.CopyGuestLiteral801(runtime, borrow.Value, literalRoots);
            ModuleLiteralRoots801? completedRoots = literalRoots;
            literalRoots = null;
            completedRoots?.Dispose();

            return new(borrow, portable);
        }
        catch (Exception original)
        {
            List<Exception>? failures = null;
            try
            {
                literalRoots?.Dispose();
            }
            catch (Exception cleanup)
            {
                (failures ??= new()).Add(cleanup);
            }
            try
            {
                borrow.Dispose();
            }
            catch (Exception cleanup)
            {
                (failures ??= new()).Add(cleanup);
            }
            if (failures is not null)
            {
                failures.Insert(0, original);
                throw new AggregateException(failures);
            }

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
        if (ImmediateTermRoot161.IsEmpty(value))
            lock (Gate)
            {
                // Same real caller admission, no ambient actor or mode flag.
                // Closing/disposed/foreign/unregistered callers keep old errors.
                if (caller is not null && IsRegisteredOpenImmediateFrame157(caller))
                    return value is Integer number ? new Integer(number.Value) : Nil.Value;
            }
        using PreparedTermTransfer506 prepared = PrepareBorrowAtomics586(Borrow(caller!, value), true);

        return prepared.Portable;
    }

    // The existing first-iteration closure admission, with identical literal,
    // ownership, immutable-shape and actual sidecar checks. Caller holds H.
    private bool TryValidateLocalClosure483(Term current)
    {
        if (IsAuthenticatedLiteral801(current)) return true;
        // Check ownership before reading a sequence; never take another
        // process heap gate while holding this process's gate.
        if (Storage(current) is { } storage)
        {
            _ = IsOwnedLocal(current); // Foreign/stale checks ALWAYS precede shortcut.
            if (current is Cons or TupleTerm or FunctionTerm or MapTerm or HamtBitmapTerm429 && storage is ProcessTermSequence506 immutableClosure
                && (immutableClosure.HasImmutablePlainClosure550 || immutableClosure.HasImmutableNoBackingClosure192)
                && CanUseImmutableClosure293(current, immutableClosure)) return true;
            if (current is Cons or TupleTerm or FunctionTerm or MapTerm or HamtBitmapTerm429
                && storage is ProcessTermSequence506 { IsMutableSequenceToken814: false } literalBlock
                && CanUseImmutableClosure293(current, literalBlock)
                && (literalBlock.HasImmutableLiteralOnlyClosure132 || literalBlock.HasImmutableReadonlyCaptureClosure138)
                && literalBlockRoots801.TryGetValue(literalBlock.Identity, out ModuleLiteralRoots801? literalCustody))
            {
                // H keeps this actual block's constructor-captured custody
                // registered until physical reclamation. Authenticate its
                // live bundle; no arena lease or new authority is acquired.
                using ModuleLiteralRoots801 held = literalCustody.Retain801();

                return true;
            }
        }

        return false;
    }

    private void ValidateLocalGraph(Term value)
    {
        if (TryValidateLocalClosure483(value)) return;
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(value);
        while (pending.TryPop(out Term? current))
        {
            if (!seen.Add(current))
                continue;
            if (TryValidateLocalClosure483(current)) continue;
            foreach (Term child in NativeMapOwnedRoots510.Children(current))
                pending.Push(child);
        }
    }

    internal Term CreateTuple(Term[] children) => children.Length == 0 ? CanonicalEmptyTuple803 : new TupleTerm(AllocateEncoded526(
        children,
        [EncodedMapHeader526.Tuple(children.Length)],
        immutablePlainClosure550: children.All(IsImmutablePlainClosure550),
        immutableLiteralOnlyClosure132: children.All(IsImmutableLiteralOnlyClosure132),
        immutableReadonlyCaptureClosure138: children.All(IsImmutableReadonlyCaptureClosure138),
        immutableNoBackingClosure192: children.All(IsImmutableNoBackingClosure192),
        immutableNoBinaryClosure192: children.All(IsImmutableNoBinaryClosure192),
        immutableLiteralEdges397: children.All(IsImmutableLiteralEdges397),
        immutableNativePublishedClosure441: children.All(IsImmutableNativePublishedClosure441)
    ));

    // No portable Tuple/list, map, float, boxed integer, identifier, function,
    // binary, resource or foreign/stale child can acquire this certificate.
    // Each physical child link is immutable after AllocateEncoded526 publishes.
    private bool IsImmutablePlainClosure550(Term value)
        => value is Nil or Atom || value is Integer && Storage(value) is null && IsImmediateInteger559(value)
            || value is Cons or TupleTerm
            && Storage(value) is ProcessTermSequence506 { HasImmutablePlainClosure550: true } storage
            && ReferenceEquals(storage.Owner, this) && IsOwnedLocal(value);

    // This proof says only that existing 524/512 literal terminals and
    // immediate values exhaust the graph. It grants NO literal authority.
    // Actual AllocateEncoded526 still captures/retains every literal edge.
    private bool IsImmutableLiteralOnlyClosure132(Term value)
    {
        if (value is Nil or Atom || value is Integer && Storage(value) is null && IsImmediateInteger559(value))
            return true;
        if (Storage(value) is RuntimeEmptyTupleStorage803)
            return IsAuthenticatedLiteral801(value);
        if (Storage(value) is not ProcessTermSequence506 block)
            return false;
        if (block.Owner is ProcessTermHeap506 owner && owner.LiteralDomain800 is { IsSealed800: true })
            return IsAuthenticatedLiteral801(value);

        // Ownership/activity is checked before consuming the constructor flag.
        return value is Cons or TupleTerm or FunctionTerm or MapTerm or HamtBitmapTerm429 && ReferenceEquals(block.Owner, this) && IsOwnedLocal(value)
            && block.HasImmutableLiteralOnlyClosure132 && CanUseImmutableClosure293(value, block);
    }

    internal Term CreateCons(Term head, Term tail)
    {
        // Only this fresh immutable Cons allocation may certify the closure.
        // No boxed head, portable tail, foreign/stale block or mutable tuple is
        // eligible. All other graphs retain the full validation walk.
        bool immediateHead = head is Nil or Atom
            || head is Integer && Storage(head) is null && IsImmediateInteger559(head);
        bool immediateClosure = immediateHead && (tail is Nil || tail is Cons
            && Storage(tail) is ProcessTermSequence506 { HasImmediateConsClosure: true } tailStorage
            && ReferenceEquals(tailStorage.Owner, this) && IsOwnedLocal(tail));

        return new Cons(AllocateEncoded526(
            [head, tail],
            [],
            immediateConsClosure: immediateClosure,
            immutablePlainClosure550: IsImmutablePlainClosure550(head) && IsImmutablePlainClosure550(tail),
            immutableLiteralOnlyClosure132: IsImmutableLiteralOnlyClosure132(head) && IsImmutableLiteralOnlyClosure132(tail),
            immutableReadonlyCaptureClosure138: IsImmutableReadonlyCaptureClosure138(head) && IsImmutableReadonlyCaptureClosure138(tail),
            immutableNoBackingClosure192: IsImmutableNoBackingClosure192(head) && IsImmutableNoBackingClosure192(tail),
            immutableNoBinaryClosure192: IsImmutableNoBinaryClosure192(head) && IsImmutableNoBinaryClosure192(tail),
            immutableLiteralEdges397: IsImmutableLiteralEdges397(head) && IsImmutableLiteralEdges397(tail),
            immutableNativePublishedClosure441: IsImmutableNativePublishedClosure441(head) && IsImmutableNativePublishedClosure441(tail)
        ));
    }

    private ProcessTermSequence506 AllocateEncoded526(
        Term[] children,
        nuint[] headers,
        nuint[]? numeric = null,
        bool immediateConsClosure = false,
        bool immutablePlainClosure550 = false,
        bool mutableSequenceToken814 = false,
        bool immutableLiteralOnlyClosure132 = false,
        bool immutableReadonlyCaptureClosure138 = false,
        bool immutableNoBackingClosure192 = false,
        bool immutableNoBinaryClosure192 = false,
        bool immutableLiteralEdges397 = false,
        bool immutableNativePublishedClosure441 = false
    )
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        ObjectDisposedException.ThrowIf(closing && frames.Count == 0, this);
        if (LiteralDomain800 is { IsSealed800: true }) throw new InvalidOperationException(ModuleLiteral800Names.Immutable);
        using ModuleLiteralRoots801? literalChildren = children.Length == 0 ? null
            : ModuleLiteralRoots801.Capture801(this, new TupleTerm(children));
        if (literalChildren is not null) literalBlockRoots801.EnsureCapacity(checked(literalBlockRoots801.Count + 1));
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
                numeric.Length,
                immediateConsClosure,
                immutablePlainClosure550,
                mutableSequenceToken814,
                immutableLiteralOnlyClosure132,
                immutableReadonlyCaptureClosure138,
                immutableNoBackingClosure192,
                immutableNoBinaryClosure192,
                literalChildren is null && !mutableSequenceToken814
                    && (immutableReadonlyCaptureClosure138 || immutableNoBackingClosure192 || immutableLiteralEdges397),
                immutableLiteralEdges397,
                immutableNativePublishedClosure441
            );
            selected.Cells.WriteHeaders526(headers, offset);
            selected.Cells.WriteNumeric556(numeric, checked(offset + headers.Length));
            selected.Cells.WriteTerms526(children, checked(offset + headers.Length + numeric.Length));
            // Reserve/retain before publishing either dictionary entry.
            ModuleLiteralRoots801? retained = literalChildren?.Retain801();
            try
            {
                if (retained is not null) literalBlockRoots801.Add(id, retained);
                blocks.Add(id, block);
            }
            catch
            {
                if (literalBlockRoots801.Remove(id, out var provisional)) provisional.Dispose();
                else retained?.Dispose();
                throw;
            }
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
        if (LiteralDomain800 is { IsSealed800: true } domain)
        {
            using var reader = domain.EnterRead800(block);

            return ReadLiteralCellCore800(block, index);
        }
        lock (Gate) return ReadLiteralCellCore800(block, index);
    }

    private Term ReadLiteralCellCore800(ProcessTermSequence506 block, int index)
    {
        if (!blocks.TryGetValue(block.Identity, out var current) || !ReferenceEquals(current, block))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, block.Count);

        return block.Slab.Cells[block.Offset + index]!;
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
        RetireFunctionEntries();
        recordDefinitions.Clear();
        RetireMagicReferences576();
        RetireResourceBinaryDescriptors577();
        RetireNativeHeaders524();
        foreach (ProcessTermSlab506 slab in slabs)
            slab.Cells.Dispose();
        blocks.Clear();
        ReleaseAllLiteralBlocks801();
        mapHeaders517.Clear();
        oldBlocks508.Clear();
        slabs.Clear();
        allocationCandidates519.Clear();
        drained.TrySetResult();
    }

    internal static IOwnedTermSequence506? Storage(Term term) => term switch
    {
        FunctionTerm function => function.OwnedFunctionStorage,
        NativeRecordTerm record => record.OwnedRecordStorage,
        TupleTerm tuple => tuple.OwnedStorage506,
        MapTerm map => map.OwnedMapPayload517?.RootPayload,
        HamtBitmapTerm429 bitmap => bitmap.OwnedStorage517,
        Cons cons => cons.OwnedStorage506,
        FloatTerm number => number.OwnedStorage556,
        Integer number => number.OwnedStorage559,
        Pid pid => pid.OwnedExternalStorage,
        PortTerm port => port.OwnedExternalStorage,
        ReferenceTerm reference => reference.OwnedExternalStorage ?? (IOwnedTermSequence506?)reference.OwnedStorage562,
        BitString binary => binary.OwnedHeapBits ?? (IOwnedTermSequence506?)binary.OwnedStorage577,
        _ => null
    };
}
