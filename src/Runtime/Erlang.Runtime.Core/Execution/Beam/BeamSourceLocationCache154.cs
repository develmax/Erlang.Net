namespace Erlang;

internal sealed class BeamSourceLocationCache154 : IDisposable
{
    private readonly ProcessContext context;
    private readonly ProcessTermHeap506 heap;
    private int references;
    internal Dictionary<Term, int> Slots154 { get; } = new(ReferenceEqualityComparer.Instance);
    internal ProcessTermFrame506 Frame154 { get; }

    internal BeamSourceLocationCache154(ProcessContext context, ProcessTermHeap506 heap)
    {
        this.context = context;
        this.heap = heap;
        // LAST fallible constructor operation. Nothing allocates after the
        // actual frame is registered and before context ownership publication.
        references = 1;
        Frame154 = heap.Enter();
    }

    private void RequireOwner154(ProcessContext owner, ProcessTermHeap506 destination)
    {
        if (!Monitor.IsEntered(heap.Gate) || !ReferenceEquals(owner, context) || !ReferenceEquals(destination, heap))
            throw new InvalidOperationException();
    }

    internal void AddReference154(ProcessContext owner, ProcessTermHeap506 destination)
    {
        RequireOwner154(owner, destination);
        ObjectDisposedException.ThrowIf(Frame154.IsDisposed, this);
        ObjectDisposedException.ThrowIf(references <= 0, this);
        int next = checked(references + 1);
        references = next;
    }

    internal bool RemoveReference154(ProcessContext owner, ProcessTermHeap506 destination)
    {
        RequireOwner154(owner, destination);
        ObjectDisposedException.ThrowIf(references <= 0, this);
        references--;

        return references == 0;
    }

    private void RequireActive154()
    {
        RequireOwner154(context, heap);
        ObjectDisposedException.ThrowIf(Frame154.IsDisposed, this);
        ObjectDisposedException.ThrowIf(references <= 0, this);
    }

    internal Term Get154(Term incoming)
    {
        int slot;
        lock (heap.Gate)
        {
            RequireActive154();
            if (Slots154.TryGetValue(incoming, out int cached)) return Frame154.Read(cached);
            slot = Frame154.Reserve();
        }
        try
        {
            // Preparation must remain outside H; strict adoption is unchanged.
            Term owned = Frame154.AdoptGuestInto507(slot, incoming);
            lock (heap.Gate)
            {
                RequireActive154();
                if (Slots154.TryGetValue(incoming, out int existing))
                {
                    Frame154.Clear(slot);

                    return Frame154.Read(existing);
                }
                Slots154.Add(incoming, slot);

                return owned;
            }
        }
        catch (Exception original)
        {
            Exception? cleanup = null;
            try
            {
                Frame154.Clear(slot);
            }
            catch (Exception failure)
            {
                cleanup = failure;
            }
            if (cleanup is not null) throw new AggregateException(original, cleanup);
            throw;
        }
    }

    internal void RetireFrame154()
    {
        // All references have gone and the context field was detached under H.
        // Frame.Dispose handles its own locking and every actual root release.
        try
        {
            Frame154.Dispose();
        }
        finally
        {
            Slots154.Clear();
        }
    }

    public void Dispose() => context.ReleaseBeamSourceLocations154(this);
}
