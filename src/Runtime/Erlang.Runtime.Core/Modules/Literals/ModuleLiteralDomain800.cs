namespace Erlang;

// The heap is the actual literal storage used by language getters. It is not a
// process heap, is never swept, and is retired only after all explicit holders
// and in-progress immutable reads release their real ownership interests.
internal sealed partial class ModuleLiteralDomain800
{
    private readonly object gate = new();
    private readonly ProcessRuntime runtime;
    private readonly ModuleRegistry registry;
    private readonly ProcessTermHeap506 heap;
    private readonly ProcessTermFrame506 roots;
    private readonly HashSet<ModuleLiteralLease800> holders = new(ReferenceEqualityComparer.Instance);
    private readonly TaskCompletionSource retired = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource readersDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private HashSet<ProcessTermSequence506>? sealedBlocks;
    private ModuleCodeImage? image;
    private IReadOnlyList<Term> literals = Array.Empty<Term>();
    private bool codeOwnerOpen = true;
    private bool preparing = true;
    private bool retiring;
    private int readers;
    private volatile bool sealed800;

    private ModuleLiteralDomain800(ModuleRegistry registry, ProcessRuntime runtime)
    {
        this.registry = registry;
        this.runtime = runtime;
        heap = ProcessTermHeap506.CreateModuleLiteral800(runtime, this);
        roots = heap.Enter();
    }

    internal bool IsSealed800 => sealed800;
    internal ProcessRuntime Runtime800 => runtime;
    internal IReadOnlyList<Term> Literals800 => literals;
    internal Task Retirement800 => retired.Task;

    internal static ModuleLiteralDomain800 Prepare800(ModuleRegistry registry, ProcessRuntime runtime, IReadOnlyList<Term> source)
    {
        var domain = new ModuleLiteralDomain800(registry, runtime);
        try
        {
            registry.OwnModuleLiteral800(domain);
            domain.Build800(source);

            return domain;
        }
        catch
        {
            _ = domain.CloseCodeOwner800();
            throw;
        }
        finally
        {
            domain.EndPreparation800();
        }
    }

    private void Build800(IReadOnlyList<Term> source)
    {
        Term[] input = source.ToArray();
        RequireSupported800(input);
        // A CLR-only preparation view groups roots for one source transaction;
        // it is never imported and contributes no invented native tuple words.
        Term preparationView = new TupleTerm(input);
        using PreparedMapGraph517 maps = heap.PrepareMapGraph517(preparationView, false);
        if (maps.Fallbacks.Count != 0)
            throw new NotSupportedException(ModuleLiteral800Names.Unsupported);
        using PreparedMapGraphUse517 admitted = maps.AdmitBuilds517();
        using ProcessTermFrame506 construction = heap.Enter();
        ProcessTermConstruction514 custody = construction.BeginConstruction514(preparationView);
        var copies = new Dictionary<Term, Term>(ReferenceEqualityComparer.Instance);
        var actual = new Term[input.Length];
        lock (heap.Gate)
        {
            for (int index = 0; index < input.Length; index++)
            {
                actual[index] = heap.AdoptModuleLiteral800(
                    input[index],
                    custody,
                    maps,
                    admitted,
                    copies
                );
                roots.RetainLocalGraph507(actual[index]);
            }
            sealedBlocks = heap.CaptureModuleLiteralBlocks800(this);
        }
        // Disposal of the construction frame drops the old CLR preparation
        // graph. Only actual arena terms remain rooted in the persistent frame.
        literals = Array.AsReadOnly(actual);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(!codeOwnerOpen || retiring, this);
            sealed800 = true;
        }
    }

    private void RequireSupported800(Term[] input)
    {
        var pending = new Stack<Term>(input);
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        while (pending.TryPop(out Term? term))
        {
            if (!seen.Add(term)) continue;
            if (ProcessTermHeap506.Storage(term) is not null)
                throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
            bool supported = term is Atom or Nil or Integer or FloatTerm or Cons
                or TupleTerm or MapTerm
                || heap.CanOwnExternalIdentifier(term)
                || term is BitString bits && (heap.CanOwnHeapBitString(bits) || heap.CanOwnModuleImmutableOrdinary805(bits))
                || term is FunctionTerm { ExternalIdentity: not null } && runtime.IsSharingLiteral425(term);
            if (!supported)
                throw new NotSupportedException(ModuleLiteral800Names.Unsupported);
            foreach (Term child in NativeMapOwnedRoots510.Children(term)) pending.Push(child);
        }
    }

    internal bool IsPreparedLiteral800(Term value)
    {
        lock (gate)
            return codeOwnerOpen && !retiring && image is null && vmConstantOwner357 is null && sealed800
                && (runtime.IsEmptyTuple803(value)
                    || ProcessTermHeap506.Storage(value) is ProcessTermSequence506 block && ContainsBlock800(block));
    }

    internal void BindImage800(ModuleRegistry owner, ModuleCodeImage codeImage)
    {
        lock (gate)
        {
            if (!ReferenceEquals(owner, registry) || !sealed800 || !codeOwnerOpen || image is not null || vmConstantOwner357 is not null)
                throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
            codeImage.BindLiteralDomain819(this);
            image = codeImage;
        }
    }

    // Called only by successful real source-code admission. The image is not
    // discovered from a bare Term; consumers derive from this admitted lease.
    internal ModuleLiteralLease800 AcquireForImage800(ModuleCodeImage codeImage)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(!codeOwnerOpen || retiring, this);
            if (!sealed800 || !ReferenceEquals(image, codeImage))
                throw new InvalidOperationException(ModuleLiteral800Names.Invalid);

            return AddHolder800();
        }
    }

    private ModuleLiteralLease800 AddHolder800()
    {
        var lease = new ModuleLiteralLease800(this);
        holders.Add(lease);

        return lease;
    }

    internal bool TryFork800(ModuleLiteralLease800 source, Term root, out ModuleLiteralLease800? lease)
    {
        lock (gate)
        {
            lease = null;
            if (source.IsDisposed || !holders.Contains(source) || retiring
                || ProcessTermHeap506.Storage(root) is not ProcessTermSequence506 block
                || !ContainsBlock800(block)) return false;
            lease = AddHolder800();

            return true;
        }
    }

    internal bool OwnsBlock800(ProcessTermSequence506 block, ModuleLiteralLease800 lease)
    {
        lock (gate)
            return !retiring && !lease.IsDisposed && holders.Contains(lease) && ContainsBlock800(block);
    }

    private bool ContainsBlock800(ProcessTermSequence506 block)
        => sealed800 && ReferenceEquals(block.Owner, heap) && sealedBlocks!.Contains(block);

    internal ModuleLiteralRead800 EnterRead800(ProcessTermSequence506 block)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            if (!ContainsBlock800(block)) throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
            int next = checked(readers + 1);
            var read = new ModuleLiteralRead800(this);
            readers = next;

            return read;
        }
    }

    internal Term ReadCell800(ProcessTermSequence506 block, int index)
    {
        using ModuleLiteralRead800 read = EnterRead800(block);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, block.Count);

        return block.Slab.Cells[block.Offset + index]!;
    }

    internal void ReleaseReader800()
    {
        lock (gate)
        {
            if (readers <= 0) throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
            if (--readers == 0 && retiring) readersDrained.TrySetResult();
        }
    }

    internal void Release800(ModuleLiteralLease800 lease)
    {
        lock (gate)
        {
            if (!holders.Remove(lease)) return;
            StartRetirement800();
        }
    }

    internal Task CloseCodeOwner800()
    {
        lock (gate)
        {
            codeOwnerOpen = false;
            StartRetirement800();

            return retired.Task;
        }
    }

    private void EndPreparation800()
    {
        lock (gate)
        {
            preparing = false;
            StartRetirement800();
        }
    }

    private void StartRetirement800()
    {
        if (codeOwnerOpen || preparing || holders.Count != 0 || retiring) return;
        retiring = true;
        if (readers == 0) readersDrained.TrySetResult();
        try
        {
            _ = Task.Run(NifImageInitializationFlow.IsolateActor(NifRetirementFlow.IsolateActor(Retire800Async)));
        }
        catch (Exception failure)
        {
            retired.TrySetException(failure);
        }
    }

    private async Task Retire800Async()
    {
        // Close/purge/last-holder disposal can run under process or module
        // metadata gates. Never release native cells on that caller's stack.
        try
        {
            await readersDrained.Task.ConfigureAwait(false);
            roots.Dispose();
            await heap.RetireAsync().ConfigureAwait(false);
            // Native completion must not require acquiring M: a documented
            // dormant direct shutdown may still retain that metadata gate.
            retired.TrySetResult();
            registry.ModuleLiteralRetired800(this);
        }
        catch (Exception failure)
        {
            retired.TrySetException(failure);
        }
    }
}
