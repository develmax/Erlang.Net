namespace Erlang;

// Storage only. This is not process/compiler/native invocation authority.
internal sealed partial class MailboxMessageFragment : IAsyncDisposable
{
    private readonly object closeGate = new();
    private readonly ProcessTermHeap506 heap;
    private readonly bool ownsHeap;
    private ProcessTermFrame506? frame;
    private Task? closing;
    internal ProcessTermHeap506 HeapOwner => heap;
    internal bool OwnsHeap => ownsHeap;
    internal Term Value { get; private set; }
    internal SequenceTraceToken? Token { get; }
    internal ProcessTermHeapSnapshot506 StorageSnapshot => heap.Snapshot;

    private MailboxMessageFragment(
        ProcessTermHeap506 heap,
        ProcessTermFrame506 frame,
        Term value,
        bool ownsHeap,
        SequenceTraceToken? token = null
    )
    {
        this.heap = heap;
        this.ownsHeap = ownsHeap;
        this.frame = frame;
        Value = value;
        Token = token;
    }

    internal static async ValueTask<MailboxMessageFragment> RetainSameHeapAsync(ProcessContext context, Term source)
    {
        ProcessTermFrame506? root = null;
        try
        {
            root = context.TermHeap506.Enter();
            root.RetainLocalGraph507(source);

            return new(
                context.TermHeap506,
                root,
                source,
                false
            );
        }
        catch (Exception primary)
        {
            try
            {
                root?.Dispose();
            }
            catch (Exception cleanup) when (!ReferenceEquals(primary, cleanup))
            {
                throw new AggregateException(primary, cleanup);
            }
            await Task.CompletedTask;
            throw;
        }
    }

    // Before R/M; caller keeps actual source custody until this returns.
    internal static ValueTask<MailboxMessageFragment> CreateAsync(ProcessRuntime runtime, Term source)
        => CreateAsync(
            runtime,
            new ProcessTermHeap506(runtime),
            source,
            true,
            false
        );

    // Only an actual recipient-preparation owner may call this before publish.
    // This does not turn a storage frame into actor/compiler invocation authority.
    internal static ValueTask<MailboxMessageFragment> CreateOnHeapAsync(ProcessContext target, Term source)
        => CreateAsync(
            target.Runtime,
            target.TermHeap506,
            source,
            false,
            false
        );

    internal static ValueTask<MailboxMessageFragment> CreatePreparedAsync(ProcessRuntime runtime,
        ProcessContext? target, Term portable) => CreatePreparedWithLiteral801Async(
            runtime,
            target,
            portable,
            null
        );

    internal static ValueTask<MailboxMessageFragment> CreatePreparedWithLiteral801Async(
        ProcessRuntime runtime,
        ProcessContext? target,
        Term portable,
        ModuleLiteralRoots801? carriedLiteral801
    )
        => CreateFragmentCore801Async(
            runtime,
            target?.TermHeap506 ?? new ProcessTermHeap506(runtime),
            portable,
            target is null,
            true,
            carriedLiteral801
        );

    // Two actual root slots: no wrapper tuple or synthetic process heap words.
    // portableToken must be covered by the caller's independent 641 transport
    // until this import finishes. The returned Token reads the copied tuple.
    internal static async ValueTask<MailboxMessageFragment> CreatePreparedToken814Async(
        ProcessRuntime runtime,
        ProcessContext? target,
        Term portable,
        SequenceTraceToken? portableToken,
        ModuleLiteralRoots801? carriedLiteral801 = null
    )
    {
        if (portableToken is null)
            return await CreatePreparedWithLiteral801Async(
                runtime,
                target,
                portable,
                carriedLiteral801
            ).ConfigureAwait(false);
        ProcessTermHeap506 heap = target?.TermHeap506 ?? new ProcessTermHeap506(runtime);
        bool ownsHeap = target is null;
        ProcessTermFrame506? frame = null;
        try
        {
            frame = heap.Enter();
            int valueSlot = frame.Reserve();
            int tokenSlot = frame.Reserve();
            frame.SetPreparedWithLiteral801(valueSlot, portable, carriedLiteral801);
            TupleTerm mutableToken = frame.PrepareMutableSequenceTokenWithLiteral814(tokenSlot, portableToken, carriedLiteral801);
            SequenceTraceToken token = SequenceTraceToken.FromPhysical(mutableToken);

            return new(
                heap,
                frame,
                frame.Read(valueSlot),
                ownsHeap,
                token
            );
        }
        catch (Exception original)
        {
            var failures = new List<Exception> { original };
            try
            {
                frame?.Dispose();
            }
            catch (Exception cleanup)
            {
                failures.Add(cleanup);
            }
            try
            {
                if (ownsHeap) await heap.RetireAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                failures.Add(cleanup);
            }
            if (failures.Count > 1) throw new AggregateException(failures);
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(original).Throw();
            throw;
        }
    }

    private static ValueTask<MailboxMessageFragment> CreateAsync(
        ProcessRuntime runtime,
        ProcessTermHeap506 heap,
        Term source,
        bool ownsHeap,
        bool alreadyPortable
    )
        => CreateFragmentCore801Async(
            runtime,
            heap,
            source,
            ownsHeap,
            alreadyPortable,
            null
        );

    private static async ValueTask<MailboxMessageFragment> CreateFragmentCore801Async(
        ProcessRuntime runtime,
        ProcessTermHeap506 heap,
        Term source,
        bool ownsHeap,
        bool alreadyPortable,
        ModuleLiteralRoots801? carriedLiteral801 = null
    )
    {
        ProcessTermFrame506? frame = null;
        try
        {
            frame = heap.Enter();
            Term portable = alreadyPortable ? source : OwnedTermDetachedCopy506.CopyAtomics586(runtime, source,
                exportHostAtomics586: false);
            int slot = frame.Reserve();
            frame.SetPreparedWithLiteral801(slot, portable, carriedLiteral801);

            // The published value IS the actual fragment-backed graph.
            return new(
                heap,
                frame,
                frame.Read(slot),
                ownsHeap
            );
        }
        catch (Exception original)
        {
            var failures = new List<Exception> { original };
            try
            {
                frame?.Dispose();
            }
            catch (Exception cleanup)
            {
                failures.Add(cleanup);
            }
            try
            {
                if (ownsHeap) await heap.RetireAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                failures.Add(cleanup);
            }
            if (failures.Count > 1) throw new AggregateException(failures);
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(original).Throw();
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (closeGate) return new(closing ??= CloseAsync());
    }

    private async Task CloseAsync()
    {
        Exception? first = null;
        try
        {
            Interlocked.Exchange(ref frame, null)?.Dispose();
        }
        catch (Exception error)
        {
            first = error;
        }
        try
        {
            if (ownsHeap) await heap.RetireAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            if (first is not null) throw new AggregateException(first, error);
            throw;
        }
        if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
        // No RecordVmCollection or word-credit operation. Storage retirement
        // is not a process GC and must never manufacture reclaimed-word stats.
    }
}
