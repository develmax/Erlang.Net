namespace Erlang;

internal sealed class RuntimeEmptyTupleDomain803
{
    private readonly object gate = new();
    private readonly NativeTermCellBuffer526 cells;
    private readonly RuntimeEmptyTupleStorage803 storage;
    private readonly HashSet<RuntimeEmptyTupleLease803> holders = new(ReferenceEqualityComparer.Instance);
    private readonly TaskCompletionSource retired = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool ownerOpen = true, retiring, reclaimQueued;
    private int readers;
    internal ProcessRuntime Runtime803 { get; }
    internal TupleTerm Value803 { get; }
    internal Task Retirement803 => retired.Task;

    internal RuntimeEmptyTupleDomain803(ProcessRuntime runtime)
    {
        Runtime803 = runtime;
        cells = new NativeTermCellBuffer526(RuntimeEmptyTuple803Names.AllocationWords);
        try
        {
            cells.WriteHeaders526([EncodedMapHeader526.Tuple(RuntimeEmptyTuple803Names.Arity)], 0);
            // PIN erl_global_literals.c allocates the following arity-0 safety
            // word too. It is padding for permitted native look-ahead, never a
            // tuple child, another logical header, or process GC payload.
            cells.WriteNumeric556([EncodedMapHeader526.Tuple(RuntimeEmptyTuple803Names.Arity)], RuntimeEmptyTuple803Names.HeaderWords);
            storage = new(this);
            Value803 = new TupleTerm(storage);
        }
        catch
        {
            cells.Dispose();
            throw;
        }
    }

    internal bool IsCanonical803(Term term)
    {
        lock (gate)
            return !retiring && ReferenceEquals(term, Value803)
                && ReferenceEquals(ProcessTermHeap506.Storage(term), storage);
    }

    // Initial admission is the actual per-VM intrinsic owner, never an image or
    // bare equal Tuple. This allocates metadata only; native storage already
    // exists before the first process context is constructed.
    internal RuntimeEmptyTupleLease803 Acquire803(RuntimeEmptyTupleStorage803 source)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(!ownerOpen || retiring, this);
            if (!ReferenceEquals(source, storage)) throw new InvalidOperationException(RuntimeEmptyTuple803Names.InvalidOwner);
            var lease = new RuntimeEmptyTupleLease803(this);
            holders.Add(lease);

            return lease;
        }
    }

    internal bool Owns803(RuntimeEmptyTupleStorage803 source, RuntimeEmptyTupleLease803 lease)
    {
        lock (gate)
            return !retiring && !lease.IsDisposed && holders.Contains(lease) && ReferenceEquals(source, storage);
    }

    internal bool IsRegisteredCaptureHolder134(ProcessRuntime expectedRuntime, RuntimeEmptyTupleLease803 lease)
    {
        lock (gate)
            return ReferenceEquals(Runtime803, expectedRuntime) && ReferenceEquals(lease.Domain, this)
                && !retiring && !lease.IsDisposed && holders.Contains(lease)
                && ReferenceEquals(storage.Domain803, this)
                && ReferenceEquals(ProcessTermHeap506.Storage(Value803), storage);
    }

    internal RuntimeEmptyTupleRead803 EnterRead803(RuntimeEmptyTupleStorage803 source)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            if (!ReferenceEquals(source, storage)) throw new InvalidOperationException(RuntimeEmptyTuple803Names.InvalidOwner);
            int next = checked(readers + 1);
            var read = new RuntimeEmptyTupleRead803(this);
            readers = next;

            return read;
        }
    }

    internal int ReadArity803(RuntimeEmptyTupleStorage803 source)
    {
        using RuntimeEmptyTupleRead803 read = EnterRead803(source);
        RequireHeader803();

        return RuntimeEmptyTuple803Names.Arity;
    }

    internal void RequireHeader803()
    {
        if (cells.Length != RuntimeEmptyTuple803Names.AllocationWords
            || cells.RootedTerms526 != 0 || cells.RawNumericWords556 != RuntimeEmptyTuple803Names.SafetyPadWords
            || cells.RawHeaders526 != RuntimeEmptyTuple803Names.HeaderWords
            || cells.ReadHeader526(0) != EncodedMapHeader526.Tuple(RuntimeEmptyTuple803Names.Arity)
            || cells.ReadNumeric556(RuntimeEmptyTuple803Names.HeaderWords) != EncodedMapHeader526.Tuple(RuntimeEmptyTuple803Names.Arity))
            throw new InvalidOperationException(RuntimeEmptyTuple803Names.InvalidHeader);
    }

    internal void Release803(RuntimeEmptyTupleLease803 lease)
    {
        lock (gate)
        {
            if (!holders.Remove(lease)) throw new InvalidOperationException(RuntimeEmptyTuple803Names.InvalidOwner);
            BeginRetirement803();
        }
    }

    internal void ReleaseRead803()
    {
        lock (gate)
        {
            if (readers <= 0) throw new InvalidOperationException(RuntimeEmptyTuple803Names.InvalidOwner);
            readers--;
            BeginRetirement803();
        }
    }

    internal Task Close803()
    {
        lock (gate)
        {
            ownerOpen = false;
            BeginRetirement803();

            return retired.Task;
        }
    }

    private void BeginRetirement803()
    {
        if (ownerOpen || holders.Count != 0) return;
        retiring = true;
        if (readers != 0 || reclaimQueued) return;
        reclaimQueued = true;
        try
        {
            // No native disposal runs inline under a process/module gate, and
            // the retirement worker inherits no actor or source-image flow.
            if (ExecutionContext.IsFlowSuppressed()) _ = Task.Run(Reclaim803);
            else using (ExecutionContext.SuppressFlow()) _ = Task.Run(Reclaim803);
        }
        catch (Exception error)
        {
            retired.TrySetException(error);
        }
    }

    private void Reclaim803()
    {
        try
        {
            cells.Dispose();
            retired.TrySetResult();
        }
        catch (Exception error)
        {
            retired.TrySetException(error);
        }
    }
}
