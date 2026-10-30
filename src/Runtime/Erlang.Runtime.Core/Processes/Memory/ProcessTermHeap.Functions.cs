namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private readonly Dictionary<long, ManagedFunctionDispatchEntry> functionEntries = new();

    private bool HasActualFunctionEnvironment(PreparedCanonicalFunction prepared)
    {
        var pending = new Stack<Term>(prepared.Environment);
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            if (Storage(value) is not null)
            {
                _ = IsOwnedLocal(value);
                foreach (Term child in NativeMapOwnedRoots510.Children(value)) pending.Push(child);
                continue;
            }
            if (runtime.IsSharingLiteral425(value) || prepared.StableGlobals.Contains(value) || value is Atom or Nil || IsImmediateInteger559(value)) continue;
            if (value is Pid pid && ReferenceEquals(pid.LocalIdentity, runtime.LocalIdentity)
                || value is PortTerm port && ReferenceEquals(port.LocalIdentity, runtime.LocalIdentity)) continue;

            // No unowned graph is copied to unused shadow env slots. A future
            // phased destination import must construct the actual callable too.
            return false;
        }

        return true;
    }

    private FunctionTerm CreateCanonicalLocalFunction(PreparedCanonicalFunction prepared, ProcessTermConstruction514 construction)
    {
        if (!Monitor.IsEntered(Gate) || !construction.Contains(prepared.Function) || !HasActualFunctionEnvironment(prepared))
            throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        FunctionTerm function = prepared.Function;
        if (function.Arity != prepared.Arity || function.Environment.Count != prepared.Environment.Length)
            throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        for (int index = 0; index < prepared.Environment.Length; index++)
            if (!ReferenceEquals(function.Environment[index], prepared.Environment[index]))
                throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        construction.PrepareFunctionTracking();
        functionEntries.EnsureCapacity(checked(functionEntries.Count + 1));
        ManagedFunctionDispatchEntry? entry = null;
        ProcessTermSequence506? block = null;
        bool bound = false;
        try
        {
            entry = new ManagedFunctionDispatchEntry(function);
            block = AllocateEncoded526(
                prepared.Environment,
                [HeapFunctionLayout.HeaderForFunction(prepared.Arity, prepared.Environment.Length, function.ExternalIdentity is not null)],
                [(nuint)entry.Address]
            );
            functionEntries.Add(block.Identity, entry);
            construction.TrackFunction(block);
            bound = function.BindHeapFunctionStorage(block);
            if (!bound) throw new InvalidOperationException(HeapFunctionLayout.Invalid);
            construction.Publish(function);

            return function;
        }
        catch
        {
            if (bound && block is not null) function.RollbackHeapFunctionStorage(block);
            if (block is not null)
            {
                functionEntries.Remove(block.Identity);
                ReleaseAllocation519(block.Slab, block.AllocationOffset526, block.AllocationWords526);
                blocks.Remove(block.Identity);
                oldBlocks508.Remove(block.Identity);
            }
            entry?.Dispose();
            throw;
        }
    }

    internal FunctionTerm CreateImportedFunction(HeapFunctionRebinding rebound, ProcessTermConstruction514 construction)
    {
        if (!Monitor.IsEntered(Gate) || !construction.Contains(rebound.Source))
            throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        foreach (Term child in rebound.Environment)
            if (!construction.Contains(child)) throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        var prepared = new PreparedCanonicalFunction(rebound.Result, rebound.Result.Arity, rebound.Environment)
        { StableGlobals = rebound.StableGlobals };
        construction.Admit(rebound.Result);
        if (prepared.Arity is < 0 or > byte.MaxValue || prepared.Environment.Length > byte.MaxValue
            || !HasActualFunctionEnvironment(prepared)) return rebound.Result;

        return CreateCanonicalLocalFunction(prepared, construction);
    }

    internal void RollbackFunctionBlocks(IReadOnlyList<ProcessTermSequence506> created)
    {
        lock (Gate)
            foreach (var block in created)
            {
                if (!functionEntries.TryGetValue(block.Identity, out var entry) || entry.Published) continue;
                entry.Resolve().RollbackHeapFunctionStorage(block);
                functionEntries.Remove(block.Identity);
                entry.Dispose();
                if (blocks.TryGetValue(block.Identity, out var current) && ReferenceEquals(current, block))
                {
                    ReleaseAllocation519(block.Slab, block.AllocationOffset526, block.AllocationWords526);
                    blocks.Remove(block.Identity);
                    oldBlocks508.Remove(block.Identity);
                }
            }
    }

    private ManagedFunctionDispatchEntry RequireFunctionEntry(ProcessTermSequence506 block, out int arity)
    {
        if (!ReferenceEquals(block.Owner, this) || !blocks.TryGetValue(block.Identity, out var live)
            || !ReferenceEquals(block, live) || !functionEntries.TryGetValue(block.Identity, out var entry))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        nuint header = block.Slab.Cells.ReadHeader526(block.AllocationOffset526);
        arity = (int)(header >> HeapFunctionLayout.ArityShift) & byte.MaxValue;
        int count = (int)(header >> HeapFunctionLayout.EnvironmentShift) & byte.MaxValue;
        if (block.HeaderWords526 != HeapFunctionLayout.HeaderWords || block.RawPayloadWords556 != HeapFunctionLayout.EntryWords
            || block.Count != count || header != HeapFunctionLayout.HeaderForFunction(arity, count, entry.Resolve().ExternalIdentity is not null)
            || block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + HeapFunctionLayout.HeaderWords) != (nuint)entry.Address)
            throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        _ = entry.Resolve();

        return entry;
    }

    internal int ReadFunctionArity(ProcessTermSequence506 block)
    {
        lock (Gate)
        {
            _ = RequireFunctionEntry(block, out int arity);

            return arity;
        }
    }

    internal IReadOnlyList<Term> ReadFunctionEnvironment(ProcessTermSequence506 block)
    {
        lock (Gate)
        {
            _ = RequireFunctionEntry(block, out _);

            return block;
        }
    }

    internal async ValueTask<Term> InvokeFunctionBody(ProcessTermSequence506 block, ITermExecutionContext context, IReadOnlyList<Term> arguments)
    {
        if (context is not ProcessContext process || !ReferenceEquals(process.Runtime, runtime)
            || !ReferenceEquals(process.TermHeap506, this) || process.CurrentOwned506 is not { IsDisposed: false } invocation)
            throw new InvalidOperationException(HeapFunctionLayout.Invalid);
        ProcessTermBorrow506 borrow;
        FunctionTerm function;
        lock (Gate)
        {
            function = RequireFunctionEntry(block, out _).Resolve();
            borrow = Borrow(invocation.Frame, function);
        }
        using (borrow)
            return await function.InvokeHeapBody(context, arguments).ConfigureAwait(false);
    }

    private void ForgetFunctionEntries(long[] dead)
    {
        foreach (long identity in dead)
            if (functionEntries.Remove(identity, out var entry)) entry.Dispose();
    }

    private void RetireFunctionEntries()
    {
        foreach (var entry in functionEntries.Values) entry.Dispose();
        functionEntries.Clear();
    }
}
