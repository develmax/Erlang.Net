using System.Runtime.InteropServices;

namespace Erlang;

// Opt-in immediate/boxed-numeric/tuple/cons fragment component. No default producer uses this type.
// Its authoritative terms are actual tagged native words, not GCHandle cells.
internal sealed unsafe class MatchSavedFragment604
{
    private readonly object gate = new();
    private readonly ProcessRuntime runtime;
    private nint memory;
    private readonly int words;
    private readonly ulong root;
    private int readers;
    private bool closing;
    private readonly TaskCompletionSource retired = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private MatchSavedFragment604(ProcessRuntime runtime, MatchFragmentPlan604 plan)
    {
        this.runtime = runtime;
        words = plan.Words;
        nuint extent = checked((nuint)MatchFragmentNames604.PrefixBytes + (nuint)words * MatchFragmentNames604.WordBytes);
        void* allocation = NativeMemory.AllocZeroed(extent);
        if (allocation == null) throw new OutOfMemoryException();
        try
        {
            var header = (MatchFragmentHeader604*)allocation;
            header->AllocatedWords = checked((nuint)words);
            header->UsedWords = checked((nuint)words);
            ulong* data = (ulong*)((byte*)allocation + MatchFragmentNames604.PrefixBytes);

            static ulong Encode(MatchFragmentPlan604 plan, ulong* data, int index)
            {
                MatchFragmentPlan604.Node node = plan.Nodes[index];

                return node.IsImmediate ? node.Immediate
                    : checked((ulong)(nuint)(data + node.Offset)) | (node.Value is Cons ? MatchFragmentNames604.ListTag : MatchFragmentNames604.BoxedTag);
            }
            for (int index = 0; index < plan.Nodes.Count; index++)
            {
                MatchFragmentPlan604.Node node = plan.Nodes[index];
                if (node.IsImmediate) continue;
                int offset = node.Offset;
                if (node.Raw is { } raw)
                {
                    foreach (ulong word in raw) data[offset++] = word;
                    continue;
                }
                if (node.Value is TupleTerm)
                    data[offset++] = checked((ulong)node.Children.Length << MatchFragmentNames604.ArityShift);
                foreach (Term child in node.Children)
                    data[offset++] = Encode(plan, data, plan.Indices[child]);
            }
            root = Encode(plan, data, plan.Root);
            memory = (nint)allocation;
        }
        catch
        {
            NativeMemory.Free(allocation);
            throw;
        }
    }

    internal static bool TryCapture604(
        ProcessRuntime runtime,
        Term value,
        CancellationToken cancellation,
        out MatchSavedFragment604? result
    )
    {
        result = null;
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64 || !BitConverter.IsLittleEndian
            || IntPtr.Size != MatchFragmentNames604.WordBytes || sizeof(MatchFragmentHeader604) != MatchFragmentNames604.PrefixBytes)
            throw new PlatformNotSupportedException(MatchFragmentNames604.Invalid);
        if (!MatchFragmentPlan604.TryCreate(
            runtime,
            value,
            cancellation,
            out MatchFragmentPlan604? plan
        )) return false;
        cancellation.ThrowIfCancellationRequested();
        result = new(runtime, plan!);

        return true;
    }

    // The returned allocation has exactly one new physical owner. This wrapper
    // is permanently closed after transfer; completion here is transfer, not free.
    internal MatchFragmentTransfer634 Transfer634(ProcessRuntime expected)
    {
        lock (gate)
        {
            if (!ReferenceEquals(runtime, expected))
                throw new ArgumentException(MatchSavedNames634.Foreign, nameof(expected));
            ObjectDisposedException.ThrowIf(closing || memory == 0, MatchFragmentNames604.Closed);
            if (readers != 0)
                throw new InvalidOperationException(MatchSavedNames634.ActiveFragmentReader);
            var result = new MatchFragmentTransfer634(memory, words, root);
            closing = true;
            memory = 0;
            retired.TrySetResult();

            return result;
        }
    }

    internal MatchFragmentReader604 Acquire604()
    {
        var reader = new MatchFragmentReader604(this);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing || memory == 0, MatchFragmentNames604.Closed);
            readers = checked(readers + 1);
        }

        return reader;
    }

    internal nint Address604 => memory;
    internal int WordCount604 => words;

    internal Term Materialize604(MatchFragmentReader604 reader, CancellationToken cancellation)
    {
        if (!reader.Owns604(this)) throw new ObjectDisposedException(MatchFragmentNames604.Closed);
        var header = (MatchFragmentHeader604*)memory;
        if (header->Next != 0 || header->OffHeapFirst != 0 || header->OffHeapOverhead != 0
            || header->AllocatedWords != (nuint)words || header->UsedWords != (nuint)words)
            throw new InvalidOperationException(MatchFragmentNames604.Invalid);

        return MatchFragmentReader604.Decode604(
            runtime,
            memory,
            words,
            root,
            cancellation
        );
    }

    internal Task Close604()
    {
        nint release;
        lock (gate)
        {
            closing = true;
            release = Detach604();
        }
        Release604(release);

        return retired.Task;
    }

    internal void ReleaseReader604()
    {
        nint release;
        lock (gate)
        {
            if (readers <= 0) throw new InvalidOperationException(MatchFragmentNames604.Invalid);
            readers--;
            release = Detach604();
        }
        Release604(release);
    }

    private nint Detach604()
    {
        if (!closing || readers != 0 || memory == 0) return 0;
        nint old = memory;
        memory = 0;

        return old;
    }

    private void Release604(nint release)
    {
        if (release == 0) return;
        // This admitted domain has no offheap objects or arbitrary destructors.
        NativeMemory.Free((void*)release);
        retired.TrySetResult();
    }
}
