using System.Runtime.InteropServices;

namespace Erlang;

// Isolated actual payload storage and graph collector. Guest Term producers do
// not use this substrate yet; its bytes must not be exposed as process_info heap.
internal sealed unsafe class ProcessOwnedTermHeap504 : IAsyncDisposable
{
    private static long nextOwner;
    private readonly long owner = Interlocked.Increment(ref nextOwner);
    private readonly object gate = new();
    private readonly Dictionary<long, ProcessOwnedTermNode504> nodes = new();
    private readonly Dictionary<long, ProcessOwnedTermRoot504> roots = new();
    private readonly TaskCompletionSource drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long nextNode;
    private long nextLease;
    private long nativeBytes;
    private bool closing;
    private bool closed;

    internal ProcessOwnedTermSnapshot504 Snapshot
    {
        get
        {
            lock (gate)
                return new(
                    nodes.Count,
                    roots.Count,
                    nativeBytes,
                    closing
                );
        }
    }

    internal ProcessOwnedTermRoot504 CreateBinary(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            nint pointer = Allocate(bytes.Length);
            try
            {
                bytes.AsSpan().CopyTo(new Span<byte>((void*)pointer, bytes.Length));

                return Publish(new(
                    ProcessOwnedTermKind504.Binary,
                    pointer,
                    bytes.Length,
                    bytes.Length
                ));
            }
            catch
            {
                NativeMemory.Free((void*)pointer);
                throw;
            }
        }
    }

    internal ProcessOwnedTermRoot504 CreateReferences(ProcessOwnedTermId504[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        // Validate and store the same snapshot even if a host mutates its array.
        children = (ProcessOwnedTermId504[])children.Clone();
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            foreach (ProcessOwnedTermId504 child in children)
                Require(child);
            long bytes = checked((long)children.Length * sizeof(long));
            nint pointer = Allocate(bytes);
            try
            {
                for (int index = 0; index < children.Length; index++)
                    ((long*)pointer)[index] = children[index].Value;

                return Publish(new(
                    ProcessOwnedTermKind504.References,
                    pointer,
                    children.Length,
                    bytes
                ));
            }
            catch
            {
                NativeMemory.Free((void*)pointer);
                throw;
            }
        }
    }

    internal ProcessOwnedTermRoot504 Root(ProcessOwnedTermId504 id)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            Require(id);
            long lease = checked(nextLease + 1);
            var root = new ProcessOwnedTermRoot504(this, lease, id);
            roots.Add(lease, root);
            nextLease = lease;

            return root;
        }
    }

    internal byte[] ReadBinary(ProcessOwnedTermRoot504 root)
    {
        lock (gate)
        {
            ProcessOwnedTermNode504 node = RequireRoot(root, ProcessOwnedTermKind504.Binary);

            return new ReadOnlySpan<byte>((void*)node.Pointer, node.Length).ToArray();
        }
    }

    internal ProcessOwnedTermId504[] ReadReferences(ProcessOwnedTermRoot504 root)
    {
        lock (gate)
        {
            ProcessOwnedTermNode504 node = RequireRoot(root, ProcessOwnedTermKind504.References);
            var result = new ProcessOwnedTermId504[node.Length];
            for (int index = 0; index < node.Length; index++)
                result[index] = new(owner, ((long*)node.Pointer)[index]);

            return result;
        }
    }

    internal void WriteReference(ProcessOwnedTermRoot504 root, int index, ProcessOwnedTermId504 value)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            ProcessOwnedTermNode504 node = RequireRoot(root, ProcessOwnedTermKind504.References);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, node.Length);
            Require(value);
            ((long*)node.Pointer)[index] = value.Value;
        }
    }

    internal ProcessOwnedTermCollection504 Collect()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            var marked = new HashSet<long>();
            var pending = new Stack<long>(roots.Values.Select(root => root.Id.Value));
            while (pending.TryPop(out long id))
            {
                if (!marked.Add(id))
                    continue;
                ProcessOwnedTermNode504 node = nodes[id];
                if (node.Kind == ProcessOwnedTermKind504.References)
                    for (int index = 0; index < node.Length; index++)
                        pending.Push(((long*)node.Pointer)[index]);
            }
            // Complete all fallible tracing/allocation before changing storage.
            long[] unreachable = nodes.Keys.Where(id => !marked.Contains(id)).ToArray();
            long reclaimed = 0;
            foreach (long id in unreachable)
            {
                ProcessOwnedTermNode504 node = nodes[id];
                reclaimed += node.Bytes;
                Free(id, node);
            }

            return new(unreachable.Length, reclaimed);
        }
    }

    internal Task RetireAsync()
    {
        lock (gate)
        {
            closing = true;
            FinishRetirement();

            return drained.Task;
        }
    }

    public ValueTask DisposeAsync() => new(RetireAsync());

    internal void Release(ProcessOwnedTermRoot504 root)
    {
        lock (gate)
        {
            if (!ReferenceEquals(root.Owner, this) || !roots.TryGetValue(root.Lease, out var current)
                || !ReferenceEquals(root, current))
                return;
            roots.Remove(root.Lease);
            FinishRetirement();
        }
    }

    private ProcessOwnedTermRoot504 Publish(ProcessOwnedTermNode504 node)
    {
        long id = checked(nextNode + 1);
        long lease = checked(nextLease + 1);
        long bytes = checked(nativeBytes + node.Bytes);
        var root = new ProcessOwnedTermRoot504(this, lease, new(owner, id));
        nodes.Add(id, node);
        try
        {
            roots.Add(lease, root);
        }
        catch
        {
            nodes.Remove(id);
            throw;
        }
        nextNode = id;
        nextLease = lease;
        nativeBytes = bytes;

        return root;
    }

    private ProcessOwnedTermNode504 Require(ProcessOwnedTermId504 id)
    {
        if (id.Owner != owner || !nodes.TryGetValue(id.Value, out var node))
            throw new ArgumentException(ProcessOwnedTermNames504.InvalidIdentity, nameof(id));

        return node;
    }

    private ProcessOwnedTermNode504 RequireRoot(ProcessOwnedTermRoot504 root, ProcessOwnedTermKind504 kind)
    {
        if (!ReferenceEquals(root.Owner, this) || !roots.TryGetValue(root.Lease, out var current)
            || !ReferenceEquals(current, root))
            throw new ObjectDisposedException(nameof(ProcessOwnedTermRoot504));
        ProcessOwnedTermNode504 node = Require(root.Id);
        if (node.Kind != kind)
            throw new InvalidOperationException(ProcessOwnedTermNames504.InvalidKind);

        return node;
    }

    private static nint Allocate(long bytes)
        => bytes == 0 ? 0 : (nint)NativeMemory.AllocZeroed(checked((nuint)bytes));

    private void Free(long id, ProcessOwnedTermNode504 node)
    {
        NativeMemory.Free((void*)node.Pointer);
        nodes.Remove(id);
        nativeBytes -= node.Bytes;
    }

    private void FinishRetirement()
    {
        if (!closing || roots.Count != 0 || closed)
            return;
        foreach (ProcessOwnedTermNode504 node in nodes.Values)
            NativeMemory.Free((void*)node.Pointer);
        nodes.Clear();
        nativeBytes = 0;
        closed = true;
        drained.TrySetResult();
    }
}
